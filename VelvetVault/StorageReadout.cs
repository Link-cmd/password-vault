using System.IO;
using System.Globalization;
namespace VelvetVault;
public static class StorageReadout {
 public static string Size(long bytes){string[] units={"B","KB","MB","GB","TB"};double value=Math.Max(0,bytes);int unit=0;while(value>=1024&&unit<units.Length-1){value/=1024;unit++;}return value.ToString(unit==0?"0":"0.#",CultureInfo.InvariantCulture)+" "+units[unit];}
 public static string Read(string path){try{long used=new FileInfo(path).Length;return $"{Size(used)} / {Size(VaultStorage.FileCapacity)} · {Size(Math.Max(0,VaultStorage.FileCapacity-used))} LEFT";}catch{return "STORAGE / UNAVAILABLE";}}
}
