// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
public static class Loc
{
    private static readonly Dictionary<string,string> english=Read();
    private static volatile string language="tr";
    public static string Language{get{return language;}}
    public static CultureInfo Culture{get{try{return CultureInfo.GetCultureInfo(LanguagePacks.Info(language).culture);}catch{return CultureInfo.GetCultureInfo("en-US");}}}
    private static Dictionary<string,string> Read(){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("Patriots.launcher.en.json")){if(s==null)throw new InvalidOperationException("Launcher language catalog missing");using(var r=new StreamReader(s))return new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(r.ReadToEnd());}}
    public static Dictionary<string,string> Catalogue{get{return new Dictionary<string,string>(english);}}
    public static string Id(string source){using(var sha=SHA256.Create())return "ui_"+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(source))).Replace("-","").Substring(0,20).ToLowerInvariant();}
    public static string Normalize(string value){string id=LanguagePacks.CanonicalId(value);return id=="en"||id=="tr"||LanguagePacks.IsInstalled(id)?id:"tr";}
    public static void Set(string value){language=Normalize(value);}
    public static string T(string source){return For(source,language);}
    public static string For(string source,string locale){if(source==null)return null;locale=Normalize(locale);if(locale=="tr")return source;string fallback;if(!english.TryGetValue(source,out fallback))fallback=source;if(locale=="en")return fallback;return LanguagePacks.Translate(source,fallback,locale);}
    public static void Check(){if(For("Görüntü","en")!="Display"||For("Görüntü","tr")!="Görüntü"||For("Yükleniyor...","en")!="Loading...")throw new Exception("Locale catalogue check failed");}
}
public static class GameLanguages
{
    public static readonly string[] Chapters={"TextGlobal","TextID_Lv1","TextID_Lv2","TextID_Lv3","TextID_Lv4","TextID_Lv5","TextID_Lv6"};
    public static bool HasGame(string language){string id=LanguagePacks.CanonicalId(language);if(id==null)return false;foreach(var chapter in Chapters)foreach(var name in new[]{"english.dat","english_nocam.dat"})if(!File.Exists(Path.Combine(Files.Engine,"languages",id,"data","localisation",chapter,name)))return false;return true;}
    public static string Resolve(string language){string id=Loc.Normalize(language);return id=="tr"||id=="en"||HasGame(id)?id:"en";}
    public static void Validate(string language){string id=Resolve(language);foreach(var c in Chapters)foreach(var f in new[]{"english.dat","english_nocam.dat"}){string rel=Path.Combine("languages",id,"data","localisation",c,f);if(!File.Exists(Path.Combine(Files.Engine,rel)))throw new FileNotFoundException(Loc.T("Oyun dosyası eksik: ")+rel);}}
}
