using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VelvetVault;
static class TransferChecks {
 public static void Run(){
  string prefix=Path.Combine(Environment.CurrentDirectory,"transfer-check-"+Guid.NewGuid().ToString("N"));string text=prefix+".txt",source=prefix+".exe",backup=prefix+".vaultbak",restored=prefix+"-restored.exe";
  try{
   var originals=new List<Entry>{new(){Name="Game 日本語",Username="user-one",Email="test@example.com",Password="transfer-secret-test-only!",Notes="first line\nsecond line"},new(){Name="Recovery",Notes="recovery-only test"}};
   AccountExport.Save(originals,text);var imported=AccountImport.Read(text);if(imported.Count!=2||!imported.Any(x=>x.Password==originals[0].Password&&x.Email==originals[0].Email&&x.Notes==originals[0].Notes))throw new Exception("Text import lost data");
   var merged=AccountImport.Merge(originals,imported);if(merged.Count!=2)throw new Exception("Exact duplicates not skipped");imported[0].Password="changed-test-only";if(AccountImport.Merge(originals,imported).Count!=3||originals[0].Password!="transfer-secret-test-only!")throw new Exception("Import overwrote existing data");
   bool invalid=false;try{AccountImport.Parse(File.ReadAllText(text).Replace("ACCOUNTS / 2","ACCOUNTS / 3"));}catch(InvalidDataException){invalid=true;}if(!invalid)throw new Exception("Incomplete text accepted");Console.WriteLine("PASS: bulk text import preserves fields, merges without overwriting, skips exact duplicates, rejects incomplete exports");
   File.WriteAllBytes(source,new byte[32]);VaultStorage.Save(source,originals,"day");VaultStorage.Backup(source,backup);var backed=VaultStorage.LoadBackup(backup);if(JsonSerializer.Serialize(backed)!=JsonSerializer.Serialize(originals)||VaultStorage.ReadTheme(backup)!="day")throw new Exception("Backup lost data or theme");if(Encoding.UTF8.GetString(File.ReadAllBytes(backup)).Contains(originals[0].Password))throw new Exception("Plaintext password in backup");
   File.WriteAllBytes(restored,new byte[16]);VaultStorage.Save(restored,backed,VaultStorage.ReadTheme(backup));if(JsonSerializer.Serialize(VaultStorage.Load(restored))!=JsonSerializer.Serialize(originals))throw new Exception("Restore lost records");
   bool wrong=false;try{VaultStorage.LoadBackup(text);}catch(InvalidDataException){wrong=true;}if(!wrong)throw new Exception("Non-backup accepted");
   using(var file=new FileStream(backup,FileMode.Open,FileAccess.ReadWrite)){file.Position=130;int b=file.ReadByte();file.Position=130;file.WriteByte((byte)(b^1));}bool corrupt=false;try{VaultStorage.LoadBackup(backup);}catch(CryptographicException){corrupt=true;}if(!corrupt)throw new Exception("Damaged backup accepted");if(VaultStorage.Load(source).Count!=2)throw new Exception("Failed restore modified source");
   Console.WriteLine("PASS: portable encrypted backup/restore preserves records and theme, rejects corruption and unrelated files");
  }finally{foreach(var path in new[]{text,source,backup,restored})if(File.Exists(path))File.Delete(path);}
 }
}
