using System.Buffers.Binary;
using System.Security.Cryptography;
namespace VelvetVault;
public sealed class TotpAccount {
 public Guid Id {get;set;}=Guid.NewGuid();public string Name {get;set;}="";public string Secret {get;set;}="";public string Algorithm {get;set;}="SHA1";public int Digits {get;set;}=6;public int Period {get;set;}=30;
}
public static class Totp {
 public static byte[] Decode(string value){
  const string alphabet="ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";string text=new(value.Where(c=>!char.IsWhiteSpace(c)&&c!='-').ToArray());text=text.TrimEnd('=').ToUpperInvariant();var output=new List<byte>();int bits=0,buffer=0;
  foreach(char c in text){int digit=alphabet.IndexOf(c);if(digit<0)throw new FormatException("Invalid setup key.");buffer=(buffer<<5)|digit;bits+=5;if(bits>=8){bits-=8;output.Add((byte)(buffer>>bits));buffer&=(1<<bits)-1;}}
  if(buffer!=0||output.Count<10)throw new FormatException("Invalid setup key.");return output.ToArray();
 }
 public static TotpAccount Parse(string name,string input){
  var account=new TotpAccount{Name=name.Trim(),Secret=input.Trim()};
  if(input.TrimStart().StartsWith("otpauth:",StringComparison.OrdinalIgnoreCase)){
   if(!Uri.TryCreate(input.Trim(),UriKind.Absolute,out var uri)||uri.Scheme!="otpauth"||uri.Host!="totp")throw new FormatException("Use a time-based authenticator setup.");
   var query=uri.Query.TrimStart('?').Split('&',StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Split('=',2)).ToDictionary(x=>Uri.UnescapeDataString(x[0]),x=>x.Length==2?Uri.UnescapeDataString(x[1]):"",StringComparer.OrdinalIgnoreCase);
   if(!query.TryGetValue("secret",out var secret))throw new FormatException();account.Secret=secret;if(account.Name.Length==0)account.Name=Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
   if(query.TryGetValue("algorithm",out var alg))account.Algorithm=alg.ToUpperInvariant();if(query.TryGetValue("digits",out var digits))account.Digits=int.Parse(digits);if(query.TryGetValue("period",out var period))account.Period=int.Parse(period);
  }
  if(account.Name.Length==0||account.Digits is not (6 or 8)||account.Period<15||account.Period>120||account.Algorithm is not ("SHA1" or "SHA256" or "SHA512"))throw new FormatException("Unsupported setup settings.");
  var key=Decode(account.Secret);CryptographicOperations.ZeroMemory(key);account.Secret=new string(account.Secret.Where(c=>!char.IsWhiteSpace(c)&&c!='-').ToArray()).TrimEnd('=').ToUpperInvariant();return account;
 }
 public static string Code(TotpAccount account,long unixSeconds){
  if(account.Digits is not (6 or 8)||account.Period<=0||unixSeconds<0)throw new FormatException();var key=Decode(account.Secret);Span<byte> counter=stackalloc byte[8];BinaryPrimitives.WriteInt64BigEndian(counter,unixSeconds/account.Period);
  try{byte[] hash=account.Algorithm switch{"SHA1"=>HMACSHA1.HashData(key,counter),"SHA256"=>HMACSHA256.HashData(key,counter),"SHA512"=>HMACSHA512.HashData(key,counter),_=>throw new FormatException()};int offset=hash[^1]&15;int number=BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset,4))&int.MaxValue;int modulo=account.Digits==8?100000000:1000000;return (number%modulo).ToString("D"+account.Digits);}
  finally{CryptographicOperations.ZeroMemory(key);}
 }
}
