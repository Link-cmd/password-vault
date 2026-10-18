using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using VelvetVault;
static class MasterChecks {
 public static void Run(){
  string generated=MasterPasswordTools.Generate();if(generated.Length!=32||!generated.Any(char.IsUpper)||!generated.Any(char.IsLower)||!generated.Any(char.IsDigit)||!generated.Any(c=>!char.IsLetterOrDigit(c)))throw new Exception("Master generator strength requirements failed");string export="master-password-export-check.txt";try{MasterPasswordTools.SaveText(export,generated);if(!File.ReadAllText(export).Contains(generated))throw new Exception("Master text export failed");}finally{if(File.Exists(export))File.Delete(export);}

  string path="master-check.tmp",backup=path+".vaultbak",updated=path+".updated";const string first="Synthetic master passphrase 123!",second="Different synthetic phrase 456!";
  try{
   var entry=new Entry{Name="Master test",Password="test-secret-only",Notes="Synthetic notes"};string key=VaultAccess.NewKey();var codes=RecoveryCodes.Generate();File.WriteAllBytes(path,new byte[32]);VaultStorage.Save(path,new List<Entry>{entry},"day",accessKey:key,recoveryHashes:codes.Select(RecoveryCodes.Hash).ToList());VaultStorage.ChangeMaster(path,"",first);if(!VaultStorage.IsMasterProtected(path))throw new Exception("Master protection not enabled");
   VaultStorage.ClearMasterSessions();bool locked=false;try{VaultStorage.Load(path);}catch(MasterLockedException){locked=true;}if(!locked)throw new Exception("Locked file decrypted without password");var unchanged=SHA256.HashData(File.ReadAllBytes(path));if(VaultStorage.UnlockMaster(path,"wrong")||!unchanged.SequenceEqual(SHA256.HashData(File.ReadAllBytes(path))))throw new Exception("Incorrect password accepted or changed file");if(!VaultStorage.UnlockMaster(path,first)||VaultStorage.LoadAccessKey(path)!=key||VaultStorage.LoadRecoveryHashes(path).Count!=10)throw new Exception("Unlock lost vault settings");
   VaultStorage.Save(path,VaultStorage.Load(path),"night");if(!VaultStorage.IsMasterProtected(path))throw new Exception("Regular save downgraded protection");VaultStorage.Backup(path,backup);VaultStorage.LockMaster(backup);locked=false;try{VaultStorage.LoadBackup(backup);}catch(MasterLockedException){locked=true;}if(!locked||!VaultStorage.UnlockMaster(backup,first)||VaultStorage.LoadBackup(backup).Single().Password!=entry.Password)throw new Exception("Backup password protection failed");
   File.WriteAllBytes(updated,new byte[64]);VaultStorage.CarryContainer(path,updated);if(!VaultStorage.IsMasterProtected(updated)||!VaultStorage.UnlockMaster(updated,first)||VaultStorage.Load(updated).Single().Password!=entry.Password)throw new Exception("Encrypted update carry failed");
   VaultStorage.ChangeMaster(updated,first,null);VaultStorage.RestoreBackup(updated,backup);if(!VaultStorage.IsMasterProtected(updated))throw new Exception("Restore downgraded protected backup");
   VaultStorage.ChangeMaster(path,first,second);VaultStorage.LockMaster(path);if(VaultStorage.UnlockMaster(path,first)||!VaultStorage.UnlockMaster(path,second))throw new Exception("Master password change failed");VaultStorage.ChangeMaster(path,second,null);VaultStorage.ClearMasterSessions();if(VaultStorage.IsMasterProtected(path)||VaultStorage.Load(path).Single().Password!=entry.Password)throw new Exception("Password-free mode restore failed");
   Console.WriteLine("PASS: optional master encryption, locked access denial, incorrect password, persistent protection, protected backup, encrypted update, change/remove, 2FA and recovery preservation");
  }finally{VaultStorage.ClearMasterSessions();foreach(string file in new[]{path,backup,updated})if(File.Exists(file))File.Delete(file);}
 }
}
