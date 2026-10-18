using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace VelvetVault;

public static partial class VaultStorage {
 public const long FileCapacity=4L*1024*1024*1024;
 static readonly byte[] Marker=Encoding.ASCII.GetBytes("PWREGISTER-V2END!");
 static readonly byte[] ProtectedMarker=Encoding.ASCII.GetBytes("PWREGISTER-V3END!");
 static readonly byte[] OldMarker=Encoding.ASCII.GetBytes("PWREGISTER-V1END!");
 const int Header=112;
 static readonly int FooterSize=8+Marker.Length;
 sealed class Container {public List<Entry> Entries {get;set;}=new();public string Theme {get;set;}="night";public List<TotpAccount> Authenticators {get;set;}=new();public string AccessKey {get;set;}="";public List<string> RecoveryHashes {get;set;}=new();}
 static (long Base,long Length,bool Legacy,bool Protected) Inspect(string path){
  using var file=File.OpenRead(path);if(file.Length<FooterSize)return(file.Length,0,false,false);file.Position=file.Length-FooterSize;using var reader=new BinaryReader(file);long length=reader.ReadInt64();var marker=reader.ReadBytes(Marker.Length);bool old=marker.SequenceEqual(OldMarker);bool locked=marker.SequenceEqual(ProtectedMarker);if(!old&&!locked&&!marker.SequenceEqual(Marker))return(file.Length,0,false,false);
  if(length<(old?60:Header+16)||length>FileCapacity||length>file.Length-FooterSize)throw new InvalidDataException("Invalid vault container.");return(file.Length-FooterSize-length,length,old,locked);
 }
 public static void CreateRunner(string path,string destination){var info=Inspect(path);using var input=File.OpenRead(path);using var output=File.Create(destination);Copy(input,output,info.Base);}
 static void Copy(Stream input,Stream output,long length){var buffer=new byte[65536];while(length>0){int count=input.Read(buffer,0,(int)Math.Min(buffer.Length,length));if(count==0)throw new EndOfStreamException();output.Write(buffer,0,count);length-=count;}}
 static byte[] Hash(Stream file,long cipherStart,long cipherLength,byte[] key,byte[] iv){using var hash=IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256,key);hash.AppendData(iv);file.Position=cipherStart;var buffer=new byte[65536];while(cipherLength>0){int n=file.Read(buffer,0,(int)Math.Min(buffer.Length,cipherLength));if(n==0)throw new EndOfStreamException();hash.AppendData(buffer,0,n);cipherLength-=n;}return hash.GetHashAndReset();}
 static Container Read(string path,MasterMaterial? supplied=null){
  var info=Inspect(path);if(info.Legacy)return new Container{Entries=LegacyVaultStorage.Load(path),Theme=LegacyVaultStorage.ReadTheme(path)};if(info.Length==0)return new();
  using var file=File.OpenRead(path);file.Position=info.Base;var aesKey=new byte[32];var authKey=new byte[32];var iv=new byte[16];var tag=new byte[32];file.ReadExactly(aesKey);file.ReadExactly(authKey);file.ReadExactly(iv);file.ReadExactly(tag);
  if(info.Protected){ValidateMasterConfig(authKey);var master=supplied??GetMaster(path,aesKey);if(!master.Salt.SequenceEqual(aesKey))throw new MasterLockedException();CryptographicOperations.ZeroMemory(aesKey);CryptographicOperations.ZeroMemory(authKey);aesKey=master.Keys[..32];authKey=master.Keys[32..];}
  try{long start=info.Base+Header,length=info.Length-Header;var actual=Hash(file,start,length,authKey,iv);if(!CryptographicOperations.FixedTimeEquals(tag,actual))throw new CryptographicException("Vault verification failed.");file.Position=start;
   using var aes=Aes.Create();aes.Key=aesKey;aes.IV=iv;using var bound=new LimitedStream(file,length);using var plain=new CryptoStream(bound,aes.CreateDecryptor(),CryptoStreamMode.Read);var result=JsonSerializer.Deserialize<Container>(plain)??throw new InvalidDataException();return result;
  }finally{CryptographicOperations.ZeroMemory(aesKey);CryptographicOperations.ZeroMemory(authKey);}
 }
 public static List<Entry> Load(string path)=>Read(path).Entries;
 public static List<TotpAccount> LoadAuthenticators(string path)=>Read(path).Authenticators;
 public static string LoadAccessKey(string path)=>Read(path).AccessKey;
 public static List<string> LoadRecoveryHashes(string path)=>Read(path).RecoveryHashes;
 public static string ReadTheme(string path)=>Read(path).Theme=="day"?"day":"night";
 public static List<Entry> LoadBackup(string path){var info=Inspect(path);if(info.Base!=0||info.Length==0)throw new InvalidDataException("Not a Vault backup.");return Load(path);}
 public static void Backup(string source,string destination){
  var info=Inspect(source);if(info.Length==0)throw new InvalidDataException("No saved vault container.");string temp=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{using(var input=File.OpenRead(source))using(var output=File.Create(temp)){input.Position=info.Base;Copy(input,output,info.Length+FooterSize);output.Flush(true);}Read(temp,info.Protected?GetMaster(source):null);File.Move(temp,destination,false);}finally{if(File.Exists(temp))File.Delete(temp);}
 }
 public static void Save(string path,List<Entry> entries,string? theme=null,List<TotpAccount>? authenticators=null,string? accessKey=null,List<string>? recoveryHashes=null)=>SaveInternal(path,entries,theme,authenticators,accessKey,recoveryHashes);
 static void SaveInternal(string path,List<Entry> entries,string? theme,List<TotpAccount>? authenticators,string? accessKey,List<string>? recoveryHashes,bool replaceProtection=false,MasterMaterial? material=null){
  var info=Inspect(path);if(info.Base+Header+40>FileCapacity)throw new IOException("Vault.exe has reached its 4 GB ceiling.");string selectedTheme=theme??ReadTheme(path);var tokens=authenticators??LoadAuthenticators(path);string gateKey=accessKey??LoadAccessKey(path);var recovery=recoveryHashes??(accessKey!=null&&accessKey!=LoadAccessKey(path)?new List<string>():LoadRecoveryHashes(path));var master=replaceProtection?material:info.Protected?GetMaster(path):null;var aesKey=master?.Keys[..32]??RandomNumberGenerator.GetBytes(32);var authKey=master?.Keys[32..]??RandomNumberGenerator.GetBytes(32);var iv=RandomNumberGenerator.GetBytes(16);string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{
   using(var output=new FileStream(temp,FileMode.CreateNew,FileAccess.ReadWrite,FileShare.None)){
    using(var input=File.OpenRead(path))Copy(input,output,info.Base);output.Write(master?.Salt??aesKey);output.Write(master!=null?MasterConfig:authKey);output.Write(iv);output.Write(new byte[32]);long start=output.Position;
    using(var aes=Aes.Create()){aes.Key=aesKey;aes.IV=iv;using var cap=new CapacityStream(output,FileCapacity-FooterSize);using var cipher=new CryptoStream(cap,aes.CreateEncryptor(),CryptoStreamMode.Write,true);JsonSerializer.Serialize(cipher,new Container{Entries=entries,Theme=selectedTheme,Authenticators=tokens,AccessKey=gateKey,RecoveryHashes=recovery});cipher.FlushFinalBlock();}
    long end=output.Position,length=end-start;var tag=Hash(output,start,length,authKey,iv);output.Position=info.Base+80;output.Write(tag);output.Position=end;
    using(var writer=new BinaryWriter(output,Encoding.UTF8,true)){writer.Write(Header+length);writer.Write(master!=null?ProtectedMarker:Marker);}output.Flush(true);
   }
   var verified=Read(temp,master);if(verified.Entries.Count!=entries.Count||verified.Authenticators.Count!=tokens.Count||verified.AccessKey!=gateKey||!verified.RecoveryHashes.SequenceEqual(recovery))throw new InvalidDataException("Write verification failed.");File.Move(temp,path,true);
  }finally{CryptographicOperations.ZeroMemory(aesKey);CryptographicOperations.ZeroMemory(authKey);if(File.Exists(temp))File.Delete(temp);}
 }
 sealed class LimitedStream(Stream inner,long remaining):Stream {
  public override int Read(byte[] buffer,int offset,int count){int n=inner.Read(buffer,offset,(int)Math.Min(count,remaining));remaining-=n;return n;}
  public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;public override long Length=>throw new NotSupportedException();public override long Position{get=>throw new NotSupportedException();set=>throw new NotSupportedException();}public override void Flush(){}public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();public override void SetLength(long v)=>throw new NotSupportedException();public override void Write(byte[] b,int o,int c)=>throw new NotSupportedException();
 }
 sealed class CapacityStream(Stream inner,long limit):Stream {
  public override void Write(byte[] buffer,int offset,int count){if(inner.Position+count>limit)throw new IOException("Vault.exe has reached its 4 GB ceiling.");inner.Write(buffer,offset,count);}
  public override bool CanRead=>false;public override bool CanSeek=>false;public override bool CanWrite=>true;public override long Length=>inner.Length;public override long Position{get=>inner.Position;set=>throw new NotSupportedException();}public override void Flush()=>inner.Flush();public override int Read(byte[] b,int o,int c)=>throw new NotSupportedException();public override long Seek(long o,SeekOrigin s)=>throw new NotSupportedException();public override void SetLength(long v)=>throw new NotSupportedException();
 }
}
