// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

public sealed class LanguageEntry {
    public string id{get;set;} public string source{get;set;} public string text{get;set;}
    public string category{get;set;} public bool editable{get;set;} public int text_id{get;set;}
    public string[] files{get;set;} public string[] required_tokens{get;set;}
}
public sealed class LanguageDocument {
    public string format{get;set;} public int template_version{get;set;}
    public string language{get;set;} public string native_name{get;set;} public string culture{get;set;} public string author{get;set;}
    public string[] instructions{get;set;} public List<LanguageEntry> entries{get;set;}
}
public sealed class LanguageInfo {
    public string id,native_name,culture;public bool launcher,game,official;public int launcherTranslated,launcherTotal,gameTranslated,gameTotal;
    public override string ToString(){return native_name;}
}
public static class LanguagePacks {
    public const string LauncherFormat="mgs-mobile-launcher-language-v1",GameFormat="mgs-mobile-game-language-v1";
    private static readonly string MetaRoot=Path.Combine(Files.Assets,"Languages");
    private static readonly string GameRoot=Path.Combine(Files.Engine,"languages");
    private static readonly Dictionary<string,Dictionary<string,string>> maps=new Dictionary<string,Dictionary<string,string>>();
    // Add another ID only when its completed launcher/game translation has been reviewed and bundled.
    private static readonly HashSet<string> OfficialIds=new HashSet<string>{"tr","en"};
    private static readonly string[] FontFiles={"common/fixed.group.bin","common/loading.group.bin","common/ui/hud.group.bin","common/codec/codec.group.bin"};
    public static string CanonicalId(string value){if(String.IsNullOrWhiteSpace(value))return null;string id=value.Trim().ToLowerInvariant();return id.Length<=35&&Regex.IsMatch(id,@"^[a-z]{2,3}(?:-[a-z0-9]{2,8})*$")&&!Regex.IsMatch(id,@"^(con|prn|aux|nul|com[1-9]|lpt[1-9])$")?id:null;}
    public static bool IsInstalled(string id){return id!=null&&(File.Exists(Path.Combine(MetaRoot,id,"launcher.json"))||File.Exists(Path.Combine(MetaRoot,id,"game.json")));}
    public static List<LanguageInfo> Available(){var list=new List<LanguageInfo>{new LanguageInfo{id="tr",native_name="Türkçe",culture="tr-TR",launcher=true,game=true,official=true},new LanguageInfo{id="en",native_name="English",culture="en-US",launcher=true,game=true,official=true}};if(Directory.Exists(MetaRoot))foreach(var dir in Directory.GetDirectories(MetaRoot)){string id=CanonicalId(Path.GetFileName(dir));if(id==null||id=="en"||id=="tr")continue;try{list.Add(Info(id));}catch{}}return list;}
    public static LanguageInfo Info(string value){string id=CanonicalId(value)??"en";if(id=="tr"||id=="en")return new LanguageInfo{id=id,native_name=id=="tr"?"Türkçe":"English",culture=id=="tr"?"tr-TR":"en-US",launcher=true,game=true,official=true};LanguageDocument ui=Load(Path.Combine(MetaRoot,id,"launcher.json")),game=Load(Path.Combine(MetaRoot,id,"game.json"));var doc=ui??game;if(doc==null)throw new InvalidDataException(Loc.T("Dil paketi bulunamadı."));return new LanguageInfo{id=id,native_name=doc.native_name,culture=doc.culture,launcher=ui!=null,game=GameLanguages.HasGame(id),official=OfficialIds.Contains(id),launcherTranslated=ui==null?0:ui.entries.Count(e=>e.editable&&!String.IsNullOrEmpty(e.text)),launcherTotal=Loc.Catalogue.Count,gameTranslated=game==null?0:game.entries.Count(e=>e.editable&&(e.category=="game_localization"||e.category=="runtime")&&!String.IsNullOrEmpty(e.text)),gameTotal=GameTemplate().entries.Count(e=>e.editable&&(e.category=="game_localization"||e.category=="runtime"))};}
    private static LanguageDocument Load(string path){return File.Exists(path)?Parse(File.ReadAllText(path,Encoding.UTF8)):null;}
    private static LanguageDocument Parse(string json){if(Encoding.UTF8.GetByteCount(json)>12*1024*1024)throw new InvalidDataException("Language file is too large.");var doc=Files.Json.Deserialize<LanguageDocument>(json.TrimStart('\ufeff'));if(doc==null||doc.entries==null)throw new InvalidDataException("Invalid language file.");return doc;}
    public static string Translate(string source,string fallback,string id){lock(maps){Dictionary<string,string> map;if(!maps.TryGetValue(id,out map)){map=new Dictionary<string,string>();try{var d=Load(Path.Combine(MetaRoot,id,"launcher.json"));if(d!=null)foreach(var e in d.entries)if(e!=null&&e.id!=null&&!String.IsNullOrEmpty(e.text))map[e.id]=e.text;}catch{map.Clear();}maps[id]=map;}string value;return map.TryGetValue(Loc.Id(source),out value)?value:fallback;}}
    public static LanguageDocument GameTemplate(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Patriots.game.template.json")){if(stream==null)throw new InvalidOperationException("Game translation template missing");using(var reader=new StreamReader(stream,Encoding.UTF8))return Parse(reader.ReadToEnd());}}
    public static LanguageDocument LauncherTemplate(){return new LanguageDocument{format=LauncherFormat,template_version=1,language="en",native_name="English",culture="en-US",author="",instructions=new[]{"Translate only text. Keep id, source, required_tokens and metadata unchanged.","Set language, native_name, culture and author. Empty text falls back to English."},entries=Loc.Catalogue.Select(p=>new LanguageEntry{id=Loc.Id(p.Key),source=p.Value,text="",category="launcher",editable=true,text_id=-1,files=new string[0],required_tokens=Tokens(p.Value)}).ToList()};}
    public static string[] Tokens(string text){return Regex.Matches(text??"",@"\{[^{}]+\}|%(?:\d+\$)?[-+#0 .\d]*[sdifuoxX]").Cast<Match>().Select(m=>m.Value).OrderBy(s=>s,StringComparer.Ordinal).ToArray();}
    private static void Validate(LanguageDocument doc,LanguageDocument template){
        string id=CanonicalId(doc.language);if(id==null||OfficialIds.Contains(id))throw new InvalidDataException(Loc.T("Yerleşik dillerin üzerine yazılamaz; farklı bir dil kodu kullan."));
        if(doc.format!=template.format||doc.template_version!=1||doc.entries.Count>10000)throw new InvalidDataException(Loc.T("Desteklenmeyen dil paketi biçimi."));
        if(String.IsNullOrWhiteSpace(doc.native_name)||doc.native_name.Length>80||doc.native_name.Any(Char.IsControl))throw new InvalidDataException(Loc.T("Dilin görünen adı geçersiz."));
        try{CultureInfo.GetCultureInfo(doc.culture);}catch{throw new InvalidDataException(Loc.T("Dil kültür kodu geçersiz."));}
        var canonical=template.entries.ToDictionary(e=>e.id,StringComparer.Ordinal);var seen=new HashSet<string>();
        foreach(var entry in doc.entries){LanguageEntry original;if(entry==null||entry.id==null||!seen.Add(entry.id)||!canonical.TryGetValue(entry.id,out original))throw new InvalidDataException(Loc.T("Bilinmeyen veya yinelenen metin kimliği."));
            if(entry.source!=original.source||entry.category!=original.category||entry.editable!=original.editable||entry.text_id!=original.text_id||!Enumerable.SequenceEqual(entry.files??new string[0],original.files??new string[0])||!Enumerable.SequenceEqual(entry.required_tokens??new string[0],original.required_tokens??new string[0]))throw new InvalidDataException(Loc.T("Kaynak metin veya korunan alan değiştirilmiş: ")+entry.id);
            string text=entry.text??"";if(text.IndexOf('\0')>=0||Encoding.UTF8.GetByteCount(text)>8191)throw new InvalidDataException(Loc.T("Metin geçersiz veya çok uzun: ")+entry.id);
            if(!entry.editable&&text.Length>0&&text!=entry.source)throw new InvalidDataException(Loc.T("Teknik kayıt çevrilemez: ")+entry.id);
            if(text.Length>0&&!Enumerable.SequenceEqual(Tokens(text),Tokens(original.source)))throw new InvalidDataException(Loc.T("Metindeki korunan işaretler değişmiş: ")+entry.id);
        }
        doc.language=id;doc.author=doc.author??"";
    }
    private static void EnsureStopped(){foreach(var process in System.Diagnostics.Process.GetProcessesByName("mgs_mobile"))using(process)try{if(String.Equals(Path.GetDirectoryName(process.MainModule.FileName),Files.Engine.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException(Loc.T("Dil paketi kurmadan önce oyunu kapat."));}catch(System.ComponentModel.Win32Exception){} }
    private static void CopyTree(string source,string dest){if(!Directory.Exists(source))return;foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories)){string relative=file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar);string to=Path.Combine(dest,relative);Directory.CreateDirectory(Path.GetDirectoryName(to));File.Copy(file,to,true);}}
    private static void DeleteOwned(string path,string root){string full=Path.GetFullPath(path),allowed=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;if(!full.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Invalid managed path");if(Directory.Exists(full))Directory.Delete(full,true);}
    private static string EntryText(LanguageEntry entry){return String.IsNullOrEmpty(entry.text)?entry.source:entry.text;}
    public static string Import(string path){
        EnsureStopped();LanguageDocument launcher=null,game=null;var fonts=new Dictionary<string,byte[]>();
        if(Path.GetExtension(path).Equals(".zip",StringComparison.OrdinalIgnoreCase)){
            using(var archive=ZipFile.OpenRead(path)){long size=0;if(archive.Entries.Count>32)throw new InvalidDataException(Loc.T("Dil paketi çok fazla dosya içeriyor."));foreach(var entry in archive.Entries){string name=entry.FullName.Replace('\\','/');if(name.EndsWith("/"))continue;size+=entry.Length;if(size>40*1024*1024||entry.Length>12*1024*1024)throw new InvalidDataException(Loc.T("Dil dosyası çok büyük."));using(var stream=entry.Open())using(var data=new MemoryStream()){byte[] buffer=new byte[16384];int read;while((read=stream.Read(buffer,0,buffer.Length))>0){if(data.Length+read>entry.Length)throw new InvalidDataException("Invalid ZIP length");data.Write(buffer,0,read);}byte[] bytes=data.ToArray();if(bytes.LongLength!=entry.Length)throw new InvalidDataException("Invalid ZIP entry");if(name=="launcher.json"&&launcher==null)launcher=Parse(Encoding.UTF8.GetString(bytes));else if(name=="game.json"&&game==null)game=Parse(Encoding.UTF8.GetString(bytes));else if(name.StartsWith("fonts/",StringComparison.Ordinal)&&FontFiles.Contains(name.Substring(6))&&!fonts.ContainsKey(name.Substring(6))){if(bytes.Length<20)throw new InvalidDataException("Invalid font group");fonts[name.Substring(6)]=bytes;}else throw new InvalidDataException(Loc.T("Dil paketinde izin verilmeyen dosya: ")+name);}}}
        }else{var doc=Parse(File.ReadAllText(path,Encoding.UTF8));if(doc.format==LauncherFormat)launcher=doc;else if(doc.format==GameFormat)game=doc;else throw new InvalidDataException(Loc.T("Desteklenmeyen dil paketi biçimi."));}
        if(launcher==null&&game==null)throw new InvalidDataException(Loc.T("Geçersiz dil dosyası."));
        if(launcher!=null)Validate(launcher,LauncherTemplate());if(game!=null)Validate(game,GameTemplate());
        string id=(launcher??game).language;if(launcher!=null&&game!=null&&launcher.language!=game.language)throw new InvalidDataException(Loc.T("Oyun ve launcher dil kodları aynı olmalı."));
        string stamp=Guid.NewGuid().ToString("N"),meta=Path.Combine(MetaRoot,id),native=Path.Combine(GameRoot,id),stageMeta=Path.Combine(MetaRoot,".stage-"+stamp),stageNative=Path.Combine(GameRoot,".stage-"+stamp),oldMeta=meta+".old-"+stamp,oldNative=native+".old-"+stamp;
        bool metaInstalled=false,nativeInstalled=false;
        try{
            Directory.CreateDirectory(stageMeta);Directory.CreateDirectory(stageNative);CopyTree(meta,stageMeta);CopyTree(native,stageNative);
            if(launcher!=null)File.WriteAllText(Path.Combine(stageMeta,"launcher.json"),Files.Json.Serialize(launcher),new UTF8Encoding(false));
            if(game!=null){BuildGame(game,stageNative);File.WriteAllText(Path.Combine(stageMeta,"game.json"),Files.Json.Serialize(game),new UTF8Encoding(false));}
            foreach(var font in fonts){string target=Path.Combine(stageNative,"data",font.Key.Replace('/',Path.DirectorySeparatorChar));Directory.CreateDirectory(Path.GetDirectoryName(target));File.WriteAllBytes(target,font.Value);}
            WriteRuntime(Load(Path.Combine(stageMeta,"launcher.json")),Load(Path.Combine(stageMeta,"game.json")),stageNative);
            if(Directory.Exists(native))Directory.Move(native,oldNative);Directory.Move(stageNative,native);nativeInstalled=true;
            if(Directory.Exists(meta))Directory.Move(meta,oldMeta);Directory.Move(stageMeta,meta);metaInstalled=true;
            lock(maps)maps.Remove(id);return id;
        }catch{
            if(metaInstalled)DeleteOwned(meta,MetaRoot);if(Directory.Exists(oldMeta))Directory.Move(oldMeta,meta);
            if(nativeInstalled)DeleteOwned(native,GameRoot);if(Directory.Exists(oldNative))Directory.Move(oldNative,native);throw;
        }finally{DeleteOwned(stageMeta,MetaRoot);DeleteOwned(stageNative,GameRoot);if(metaInstalled&&nativeInstalled){DeleteOwned(oldMeta,MetaRoot);DeleteOwned(oldNative,GameRoot);}}
    }
    private static void BuildGame(LanguageDocument doc,string destination){
        var changes=new Dictionary<string,Dictionary<int,string>>();
        foreach(var entry in doc.entries.Where(e=>e.editable&&e.category=="game_localization"))foreach(string file in entry.files){Dictionary<int,string> table;if(!changes.TryGetValue(file,out table))changes[file]=table=new Dictionary<int,string>();table[entry.text_id]=EntryText(entry);}
        foreach(string chapter in GameLanguages.Chapters)foreach(string name in new[]{"english.dat","english_nocam.dat"}){
            string relative="localisation/"+chapter+"/"+name,original=Path.Combine(GameRoot,"en","data",relative.Replace('/',Path.DirectorySeparatorChar));if(!File.Exists(original))throw new FileNotFoundException(Loc.T("Oyun dosyası eksik: ")+original);
            Dictionary<int,string> replacements;if(!changes.TryGetValue(relative,out replacements))replacements=new Dictionary<int,string>();byte[] bytes=RebuildDat(File.ReadAllBytes(original),replacements);
            string target=Path.Combine(destination,"data",relative.Replace('/',Path.DirectorySeparatorChar));Directory.CreateDirectory(Path.GetDirectoryName(target));File.WriteAllBytes(target,bytes);
        }
    }
    public static byte[] RebuildDat(byte[] original,Dictionary<int,string> replacements){
        if(original.Length<21)throw new InvalidDataException("Invalid game text table");string magic=Encoding.ASCII.GetString(original,0,4);if(magic!="GLB0"&&magic!="PRT0")throw new InvalidDataException("Invalid game text table");uint count=BitConverter.ToUInt32(original,16);if(count>65536||20L+count*6>=original.Length)throw new InvalidDataException("Invalid game text index");int header=20+(int)count*6,end=Array.IndexOf(original,(byte)0,header);if(end<header||end-header>16)throw new InvalidDataException("Invalid game text encoding");string encoding=Encoding.ASCII.GetString(original,header,end-header);if(!encoding.Equals("UTF-8",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Unsupported game text encoding");var offsets=new List<Tuple<ushort,uint>>();
        using(var pool=new MemoryStream())using(var result=new MemoryStream())using(var writer=new BinaryWriter(result)){
            for(int i=0;i<count;i++){ushort id=BitConverter.ToUInt16(original,20+i*6);uint offset=BitConverter.ToUInt32(original,22+i*6);long start=end+1L+offset;if(start>=original.Length)throw new InvalidDataException("Invalid game string offset");int zero=Array.IndexOf(original,(byte)0,(int)start);if(zero<0)throw new InvalidDataException("Invalid game string terminator");string value;byte[] raw;if(replacements.TryGetValue(id,out value))raw=Encoding.UTF8.GetBytes(value);else raw=original.Skip((int)start).Take(zero-(int)start).ToArray();offsets.Add(Tuple.Create(id,(uint)pool.Length));pool.Write(raw,0,raw.Length);pool.WriteByte(0);}
            writer.Write(original,0,8);writer.Write((uint)pool.Length);writer.Write(BitConverter.ToUInt32(original,12));writer.Write(count);foreach(var row in offsets){writer.Write(row.Item1);writer.Write(row.Item2);}writer.Write(Encoding.ASCII.GetBytes(encoding));writer.Write((byte)0);writer.Write(pool.ToArray());return result.ToArray();
        }
    }
    private static void WriteRuntime(LanguageDocument ui,LanguageDocument game,string folder){
        var strings=new Dictionary<string,string>();if(ui!=null)foreach(var e in ui.entries.Where(e=>e.editable&&!String.IsNullOrEmpty(e.text)))strings[e.source]=e.text;if(game!=null)foreach(var e in game.entries.Where(e=>e.editable&&!String.IsNullOrEmpty(e.text)))strings[e.source]=e.text;
        using(var stream=File.Create(Path.Combine(folder,"runtime.bin")))using(var writer=new BinaryWriter(stream)){writer.Write(Encoding.ASCII.GetBytes("PMLOC1\0\0"));writer.Write(strings.Count);foreach(var pair in strings){foreach(string s in new[]{pair.Key,pair.Value}){byte[] b=Encoding.UTF8.GetBytes(s);writer.Write(b.Length);writer.Write(b);}}}
    }
    public static void ExportKit(string folder,string target="pt-br"){
        Directory.CreateDirectory(folder);foreach(var pair in new[]{Tuple.Create("launcher",LauncherTemplate()),Tuple.Create("game",GameTemplate())}){string dir=Path.Combine(folder,pair.Item1);Directory.CreateDirectory(dir);var doc=pair.Item2;doc.language=CanonicalId(target)??"pt-br";doc.native_name=doc.language=="pt-br"?"Português (Brasil)":doc.language;doc.culture=doc.language=="pt-br"?"pt-BR":"en-US";foreach(var e in doc.entries)e.text="";File.WriteAllText(Path.Combine(dir,doc.language+"."+pair.Item1+".json"),Files.Json.Serialize(doc),new UTF8Encoding(false));}
    }
}
