using System.IO;
using System.Text;
namespace VelvetVault;
public static class AccountExport {
 public static void Save(IEnumerable<Entry> entries,string path){
  var accounts=entries.OrderBy(x=>x.Name,StringComparer.OrdinalIgnoreCase).ToList();
  var text=new StringBuilder("VAULT / SAVED ACCOUNTS\r\n").Append("ACCOUNTS / ").Append(accounts.Count).Append("\r\n\r\n");
  foreach(var item in accounts){text.AppendLine(new string('=',48));text.Append("NAME: ").AppendLine(item.Name);text.Append("USERNAME: ").AppendLine(item.Username);text.Append("EMAIL: ").AppendLine(item.Email);text.Append("PASSWORD: ").AppendLine(item.Password);text.Append("WEBSITE: ").AppendLine(item.Website);text.AppendLine("RECOVERY DETAILS:");text.AppendLine(item.Notes);text.AppendLine();}
  string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{File.WriteAllText(temp,text.ToString(),new UTF8Encoding(false));File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}
 }
}
