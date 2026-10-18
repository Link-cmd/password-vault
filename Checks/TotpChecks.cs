using System.IO;
using VelvetVault;
static class TotpChecks {
 public static void Run(){
  var account=new TotpAccount{Name="RFC test",Secret="GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",Digits=8};
  long[] times={59,1111111109,1111111111,1234567890,2000000000,20000000000};string[] expected={"94287082","07081804","14050471","89005924","69279037","65353130"};for(int i=0;i<times.Length;i++)if(Totp.Code(account,times[i])!=expected[i])throw new Exception("RFC 6238 SHA1 vector failed");
  account.Algorithm="SHA256";account.Secret="GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQGEZA====";if(Totp.Code(account,59)!="46119246")throw new Exception("RFC SHA256 vector failed");
  var uri=Totp.Parse("","otpauth://totp/Example%3Atest?secret=JBSWY3DPEHPK3PXP&issuer=Example&digits=6&period=30");if(uri.Name!="Example:test"||Totp.Code(uri,59).Length!=6)throw new Exception("Authenticator URI failed");bool invalid=false;try{Totp.Parse("Bad","not-a-key!");}catch(FormatException){invalid=true;}if(!invalid)throw new Exception("Invalid setup accepted");
  Console.WriteLine("PASS: official RFC 6238 vectors, authenticator setup URI, invalid-key rejection");
  string file="totp-storage-check.tmp",backup="totp-backup-check.vaultbak";try{File.WriteAllBytes(file,new byte[32]);VaultStorage.Save(file,new List<Entry>(),"night",new List<TotpAccount>{uri});VaultStorage.Save(file,new List<Entry>{new(){Name="Separate password",Password="test-only"}},"day");if(VaultStorage.LoadAuthenticators(file).Single().Secret!=uri.Secret)throw new Exception("Password/theme save lost authenticator");VaultStorage.Backup(file,backup);if(VaultStorage.LoadAuthenticators(backup).Single().Secret!=uri.Secret)throw new Exception("Backup lost authenticator");Console.WriteLine("PASS: authenticator setups persist across password/theme saves and encrypted backups");}finally{foreach(string path in new[]{file,backup})if(File.Exists(path))File.Delete(path);}
 }
}
