// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
public static class Loc
{
    private static readonly Dictionary<string,string> english=Read();
    private static volatile string language="tr";
    public static string Language{get{return language;}}
    public static CultureInfo Culture{get{return CultureInfo.GetCultureInfo(language=="en"?"en-US":"tr-TR");}}
    private static Dictionary<string,string> Read(){using(var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("Patriots.launcher.en.json")){if(s==null)throw new InvalidOperationException("Launcher language catalog missing");using(var r=new StreamReader(s))return new JavaScriptSerializer().Deserialize<Dictionary<string,string>>(r.ReadToEnd());}}
    public static string Normalize(string value){return String.Equals(value,"en",StringComparison.OrdinalIgnoreCase)?"en":"tr";}
    public static void Set(string value){language=Normalize(value);}
    public static string T(string source){return For(source,language);}
    public static string For(string source,string locale){if(source==null)return null;string result;return locale=="en"&&english.TryGetValue(source,out result)?result:source;}
    public static void Check(){if(For("Görüntü","en")!="Display"||For("Görüntü","tr")!="Görüntü"||For("Yükleniyor...","en")!="Loading...")throw new Exception("Locale catalogue check failed");}
}
public static class GameLanguages
{
    public static void Validate(string language){string[] chapters={"TextGlobal","TextID_Lv1","TextID_Lv2","TextID_Lv3","TextID_Lv4","TextID_Lv5","TextID_Lv6"};foreach(var c in chapters)foreach(var f in new[]{"english.dat","english_nocam.dat"}){string rel=Path.Combine("languages",Loc.Normalize(language),"data","localisation",c,f);if(!File.Exists(Path.Combine(Files.Engine,rel)))throw new FileNotFoundException(Loc.T("Oyun dosyası eksik: ")+rel);}}
}
