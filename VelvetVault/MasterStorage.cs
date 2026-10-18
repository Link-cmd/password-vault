using System.IO;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
namespace VelvetVault;
public sealed class MasterLockedException:CryptographicException {public MasterLockedException():base("Enter the master password to open this vault."){} }
public static partial class VaultStorage {
 sealed class MasterMaterial:IDisposable {public required byte[] Salt;public required byte[] Keys;public void Dispose(){CryptographicOperations.ZeroMemory(Keys);}}
 static readonly Dictionary<string,MasterMaterial> masterSessions=new(StringComparer.OrdinalIgnoreCase);
 static readonly byte[] MasterConfig=Encoding.ASCII.GetBytes("ARGON2ID-65536-3-2".PadRight(32,'\0'));
 static void ValidateMasterConfig(byte[] config){if(!config.SequenceEqual(MasterConfig))throw new InvalidDataException("Unsupported master-password settings.");}
 static MasterMaterial Derive(string password,byte[] salt){byte[] bytes=Encoding.UTF8.GetBytes(password);try{using var argon=new Argon2id(bytes){Salt=salt,MemorySize=65536,Iterations=3,DegreeOfParallelism=2};return new MasterMaterial{Salt=salt,Keys=argon.GetBytes(64)};}finally{CryptographicOperations.ZeroMemory(bytes);}}
 static MasterMaterial GetMaster(string path,byte[]? salt=null){if(!masterSessions.TryGetValue(Path.GetFullPath(path),out var material)||(salt!=null&&!material.Salt.SequenceEqual(salt)))throw new MasterLockedException();return material;}
 public static bool IsMasterProtected(string path)=>Inspect(path).Protected;
 public static void LockMaster(string path){if(masterSessions.Remove(Path.GetFullPath(path),out var material))material.Dispose();}
 public static void ClearMasterSessions(){foreach(var material in masterSessions.Values)material.Dispose();masterSessions.Clear();}
 public static bool UnlockMaster(string path,string password){
  var info=Inspect(path);if(!info.Protected)return true;using var input=File.OpenRead(path);input.Position=info.Base;byte[] salt=new byte[32],config=new byte[32];input.ReadExactly(salt);input.ReadExactly(config);ValidateMasterConfig(config);var material=Derive(password,salt);
  try{Read(path,material);LockMaster(path);masterSessions[Path.GetFullPath(path)]=material;return true;}catch(CryptographicException){material.Dispose();return false;}catch{material.Dispose();throw;}
 }
 public static void ChangeMaster(string path,string currentPassword,string? newPassword){
  if(IsMasterProtected(path)&&!UnlockMaster(path,currentPassword))throw new CryptographicException("Current master password did not match.");if(newPassword!=null&&(newPassword.Length<12||string.IsNullOrWhiteSpace(newPassword)))throw new ArgumentException("Use at least 12 characters for your master password.");
  var data=Read(path);MasterMaterial? material=newPassword==null?null:Derive(newPassword,RandomNumberGenerator.GetBytes(32));
  try{SaveInternal(path,data.Entries,data.Theme,data.Authenticators,data.AccessKey,data.RecoveryHashes,true,material);LockMaster(path);if(material!=null)masterSessions[Path.GetFullPath(path)]=material;}catch{material?.Dispose();throw;}
 }
 public static void CarryContainer(string source,string cleanExecutable){
  var info=Inspect(source);if(info.Length==0)return;var target=Inspect(cleanExecutable);if(target.Length!=0)throw new InvalidDataException("Update target must be a clean executable.");if(target.Base+info.Length+FooterSize>FileCapacity)throw new IOException("Updated vault exceeds its capacity.");using var input=File.OpenRead(source);using var output=new FileStream(cleanExecutable,FileMode.Open,FileAccess.Write);input.Position=info.Base;output.Position=target.Base;Copy(input,output,info.Length+FooterSize);output.Flush(true);
 }
 public static void RestoreBackup(string destination,string backup){
  LoadBackup(backup);var data=Read(backup);var current=IsMasterProtected(destination)?GetMaster(destination):IsMasterProtected(backup)?GetMaster(backup):null;
  MasterMaterial? material=current==null?null:new MasterMaterial{Salt=current.Salt.ToArray(),Keys=current.Keys.ToArray()};
  try{SaveInternal(destination,data.Entries,data.Theme,data.Authenticators,data.AccessKey,data.RecoveryHashes,true,material);LockMaster(destination);if(material!=null)masterSessions[Path.GetFullPath(destination)]=material;}catch{material?.Dispose();throw;}
 }
}
