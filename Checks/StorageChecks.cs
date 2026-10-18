using System.IO;
using System.Reflection;
using VelvetVault;
static class StorageChecks {
 public static void Run(){
  string path=Path.Combine(Environment.CurrentDirectory,"large-storage-check.tmp");
  try{
   File.WriteAllBytes(path,new byte[32]);
   var legacy=typeof(VaultStorage).Assembly.GetType("VelvetVault.LegacyVaultStorage")!;
   legacy.GetMethod("Save")!.Invoke(null,new object[]{path,new List<Entry>{new(){Name="Legacy test",Password="test-only"}},"day"});
   if(VaultStorage.Load(path)[0].Name!="Legacy test"||VaultStorage.ReadTheme(path)!="day")throw new Exception("Legacy compatibility failed");
   VaultStorage.Save(path,VaultStorage.Load(path));if(VaultStorage.ReadTheme(path)!="day")throw new Exception("Migration lost theme");
   Console.WriteLine("PASS: legacy container migrates with credentials and theme intact");
   var entry=new Entry{Name="Large synthetic test",Password="test-only",Notes=new string('x',65*1024*1024)};
   VaultStorage.Save(path,new List<Entry>{entry},"night");var loaded=VaultStorage.Load(path);if(loaded[0].Notes.Length!=entry.Notes.Length||loaded[0].Notes[^1]!='x')throw new Exception("Large vault round trip failed");
   if(new FileInfo(path).Length<=64L*1024*1024)throw new Exception("Large test did not cross previous limit");
   Console.WriteLine("PASS: authenticated save/load exceeds old 64 MB limit using streamed storage");
   using(var file=new FileStream(path,FileMode.Open,FileAccess.ReadWrite)){file.Position=160;int b=file.ReadByte();file.Position=160;file.WriteByte((byte)(b^1));}
   bool rejected=false;try{VaultStorage.Load(path);}catch(System.Security.Cryptography.CryptographicException){rejected=true;}if(!rejected)throw new Exception("Corrupt data accepted");
   if(VaultStorage.FileCapacity!=4L*1024*1024*1024||StorageReadout.Size(VaultStorage.FileCapacity)!="4 GB")throw new Exception("Capacity readout failed");
   Console.WriteLine("PASS: altered ciphertext rejected and 4 GB ceiling readout verified");
  }finally{if(File.Exists(path))File.Delete(path);}
 }
}
