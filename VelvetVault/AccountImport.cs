using System.IO;
using System.Text.RegularExpressions;
namespace VelvetVault;
public static class AccountImport {
 public static List<Entry> Read(string path){
  if(new FileInfo(path).Length>64L*1024*1024)throw new InvalidDataException("Text file too large.");return Parse(File.ReadAllText(path));
 }
 public static List<Entry> Parse(string text){
  text=text.Replace("\r\n","\n").Replace("\r","\n");
  if(!text.StartsWith("VAULT / SAVED ACCOUNTS\n"))throw new InvalidDataException("Choose a Vault text export.");
  var header=Regex.Match(text,@"^ACCOUNTS / (\d+)$",RegexOptions.Multiline);if(!header.Success||!int.TryParse(header.Groups[1].Value,out int expected))throw new InvalidDataException("Missing account count.");
  var blocks=Regex.Split(text,@"(?m)^={48}\n(?=NAME: )");var result=new List<Entry>();
  foreach(string block in blocks.Skip(1)){
   var match=Regex.Match(block,@"\ANAME: ([^\n]*)\nUSERNAME: ([^\n]*)\nEMAIL: ([^\n]*)\nPASSWORD: ([\s\S]*?)\nWEBSITE: ([^\n]*)\nRECOVERY DETAILS:\n([\s\S]*)\z");if(!match.Success)throw new InvalidDataException("Unrecognized account layout.");
   string notes=match.Groups[6].Value;if(notes.EndsWith("\n\n"))notes=notes[..^2];
   var entry=new Entry{Name=match.Groups[1].Value,Username=match.Groups[2].Value,Email=match.Groups[3].Value,Password=match.Groups[4].Value,Website=match.Groups[5].Value,Notes=notes};if(string.IsNullOrWhiteSpace(entry.Name))throw new InvalidDataException("Account name missing.");result.Add(entry);
  }
  if(result.Count!=expected)throw new InvalidDataException("Account count does not match the file.");return result;
 }
 public static List<Entry> Merge(IEnumerable<Entry> current,IEnumerable<Entry> incoming){var next=current.ToList();foreach(var entry in incoming){if(next.Any(x=>x.Name==entry.Name&&x.Username==entry.Username&&x.Email==entry.Email&&x.Password==entry.Password&&x.Website==entry.Website&&x.Notes==entry.Notes))continue;next.Add(new Entry{Id=Guid.NewGuid(),Name=entry.Name,Username=entry.Username,Email=entry.Email,Password=entry.Password,Website=entry.Website,Notes=entry.Notes,Category=entry.Category});}return next;}
}
