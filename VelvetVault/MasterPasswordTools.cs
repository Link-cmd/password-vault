using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace VelvetVault;
public static class MasterPasswordTools {
 public static string Generate(){const string chars="ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%&*+-?";string value;do{value=new string(Enumerable.Range(0,32).Select(_=>chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());}while(!value.Any(char.IsUpper)||!value.Any(char.IsLower)||!value.Any(char.IsDigit)||!value.Any(c=>!char.IsLetterOrDigit(c)));return value;}
 public static void SaveText(string path,string password){
  if(!Path.GetExtension(path).Equals(".txt",StringComparison.OrdinalIgnoreCase)||password.Length<12||string.IsNullOrWhiteSpace(password))throw new ArgumentException("Choose a .txt file for a valid master password.");
  string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{File.WriteAllText(temp,"VAULT / MASTER PASSWORD"+Environment.NewLine+"This file contains the password in plain text. Store it separately from Vault."+Environment.NewLine+"Use ENABLE or CHANGE in Vault to apply a new password."+Environment.NewLine+Environment.NewLine+password+Environment.NewLine,new UTF8Encoding(false));File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}
 }
}
