// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

public static class LanguagePackChecks {
    private static void Require(bool value,string message){if(!value)throw new Exception("Language-pack test: "+message);}
    private static string Save(string name,LanguageDocument doc){string path=Path.Combine(Files.Root,name);File.WriteAllText(path,Files.Json.Serialize(doc),new UTF8Encoding(false));return path;}
    private static void Reject(Action action,string message){try{action();}catch(InvalidDataException){return;}throw new Exception("Language-pack test did not reject: "+message);}
    private static void Headers(LanguageDocument doc,string id){doc.language=id;doc.native_name="Português (teste)";doc.culture="pt-BR";doc.author="Isolated test fixture";}
    private static void MakeEnglish(){
        var doc=LanguagePacks.GameTemplate();
        foreach(string chapter in GameLanguages.Chapters)foreach(string name in new[]{"english.dat","english_nocam.dat"}){
            string relative="localisation/"+chapter+"/"+name;
            var rows=doc.entries.Where(e=>e.files.Contains(relative)).OrderBy(e=>e.text_id).ToArray();
            using(var pool=new MemoryStream())using(var table=new MemoryStream())using(var index=new BinaryWriter(table)){
                foreach(var e in rows){index.Write((ushort)e.text_id);index.Write((uint)pool.Length);byte[] b=Encoding.UTF8.GetBytes(e.source);pool.Write(b,0,b.Length);pool.WriteByte(0);}
                string file=Path.Combine(Files.Engine,"languages","en","data",relative.Replace('/',Path.DirectorySeparatorChar));Directory.CreateDirectory(Path.GetDirectoryName(file));
                using(var stream=File.Create(file))using(var writer=new BinaryWriter(stream)){writer.Write(Encoding.ASCII.GetBytes(chapter=="TextGlobal"?"GLB0":"PRT0"));writer.Write(47110);writer.Write((uint)pool.Length);writer.Write(1722);writer.Write(rows.Length);writer.Write(table.ToArray());writer.Write(Encoding.ASCII.GetBytes("UTF-8\0"));writer.Write(pool.ToArray());}
            }
        }
    }
    private static string ReadDat(byte[] bytes,int id){int n=BitConverter.ToInt32(bytes,16),encoding=20+n*6,start=Array.IndexOf(bytes,(byte)0,encoding)+1;for(int i=0;i<n;i++)if(BitConverter.ToUInt16(bytes,20+i*6)==id){int off=(int)BitConverter.ToUInt32(bytes,22+i*6)+start,end=Array.IndexOf(bytes,(byte)0,off);return Encoding.UTF8.GetString(bytes,off,end-off);}throw new Exception("Missing text ID");}
    public static int Run(bool worker){
        if(!worker){string temp=Path.Combine(Path.GetTempPath(),"Patriots-Languages-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);try{string exe=Path.Combine(temp,"Metal Gear Solid Mobile.exe");File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location,exe);using(var process=Process.Start(new ProcessStartInfo(exe,"--test-language-packs-worker"){UseShellExecute=false,CreateNoWindow=true})){if(!process.WaitForExit(60000)){process.Kill();throw new Exception("Language-pack test timed out");}Require(process.ExitCode==0,"isolated worker failed");}string qa=Path.Combine(Files.Root,"QA");Directory.CreateDirectory(qa);File.Copy(Path.Combine(temp,"test-result.json"),Path.Combine(qa,"language-packs.json"),true);return 0;}finally{string full=Path.GetFullPath(temp),root=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;if(full.StartsWith(root,StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(full).StartsWith("Patriots-Languages-"))Directory.Delete(full,true);}}
        MakeEnglish();Loc.Check();var ui=LanguagePacks.LauncherTemplate();Headers(ui,"pt-qa");
        ui.entries.First(e=>e.source=="Display").text="Vídeo";
        ui.entries.First(e=>e.source=="Loading...").text="Carregando...";
        Require(LanguagePacks.Import(Save("launcher.json",ui))=="pt-qa","launcher import ID");
        Require(GameLanguages.Resolve("pt-qa")=="en","launcher-only English fallback");Loc.Set("pt-qa");Require(Loc.T("Görüntü")=="Vídeo","launcher Unicode translation");Require(Loc.Culture.Name=="pt-BR","culture");
        var game=LanguagePacks.GameTemplate();Headers(game,"pt-qa");game.entries.First(e=>e.id=="MGS_0002_01").text="Cancelar";
        var prompt=game.entries.First(e=>e.editable&&e.category=="game_localization"&&e.source.Contains("{pc:a}"));prompt.text="AÇÃO "+prompt.source;
        var loading=game.entries.First(e=>e.category=="runtime"&&e.source=="Loading...");loading.text="Carregando...";
        LanguagePacks.Import(Save("game.json",game));Require(GameLanguages.Resolve("pt-qa")=="pt-qa","game component active");GameLanguages.Validate("pt-qa");
        string installed=Path.Combine(Files.Engine,"languages","pt-qa","data","localisation","TextGlobal","english.dat");Require(ReadDat(File.ReadAllBytes(installed),2)=="Cancelar","rebuilt UTF-8 DAT text");string hash=Files.Hash(installed);
        string table=prompt.files.First();Require(ReadDat(File.ReadAllBytes(Path.Combine(Files.Engine,"languages","pt-qa","data",table.Replace('/',Path.DirectorySeparatorChar))),prompt.text_id).Contains("{pc:a}"),"dynamic action token preserved");
        Require(File.Exists(Path.Combine(Files.Engine,"languages","pt-qa","runtime.bin")),"native runtime catalogue");
        var bad=LanguagePacks.GameTemplate();Headers(bad,"pt-qa");bad.entries.First(e=>!e.editable).text="EDITED";Reject(()=>LanguagePacks.Import(Save("bad.json",bad)),"protected font record");Require(Files.Hash(installed)==hash,"rejected import leaves previous pack intact");
        bad=LanguagePacks.GameTemplate();Headers(bad,"pt-qa");bad.entries.First(e=>e.source.Contains("{pc:a}")).text="lost token";Reject(()=>LanguagePacks.Import(Save("bad.json",bad)),"missing button token");
        bad=LanguagePacks.GameTemplate();Headers(bad,"pt-qa");bad.entries[2].source="CHANGED";Reject(()=>LanguagePacks.Import(Save("bad.json",bad)),"source mutation");
        bad=LanguagePacks.GameTemplate();Headers(bad,"pt-qa");bad.entries.Add(bad.entries[2]);Reject(()=>LanguagePacks.Import(Save("bad.json",bad)),"duplicate ID");
        var core=LanguagePacks.LauncherTemplate();Headers(core,"en");Reject(()=>LanguagePacks.Import(Save("core.json",core)),"built-in overwrite");
        Headers(core,"../bad");Reject(()=>LanguagePacks.Import(Save("core.json",core)),"locale path traversal");
        string zip=Path.Combine(Files.Root,"bad.zip");using(var archive=ZipFile.Open(zip,ZipArchiveMode.Create)){using(var w=new StreamWriter(archive.CreateEntry("../escaped.json").Open()))w.Write("{}");}Reject(()=>LanguagePacks.Import(zip),"ZIP traversal");
        var ui2=LanguagePacks.LauncherTemplate();Headers(ui2,"pt-qa");ui2.entries.First(e=>e.source=="Display").text="Tela";LanguagePacks.Import(Save("update.json",ui2));Require(Loc.T("Görüntü")=="Tela","live UI catalogue cache reload");Require(Files.Hash(installed)==hash,"launcher update preserves game tables");
        LanguagePacks.ExportKit(Path.Combine(Files.Root,"Exports"));Require(File.Exists(Path.Combine(Files.Root,"Exports","launcher","pt-br.launcher.json")),"translator export");
        Require(GameLanguages.HasGame("en"),"built-in English tables preserved");Require(LanguagePacks.Available().Count==3,"third community locale discovered");
        string bundle=Path.Combine(Files.Root,"bundle.zip");using(var archive=ZipFile.Open(bundle,ZipArchiveMode.Create)){foreach(var pair in new[]{Tuple.Create("launcher.json",ui2),Tuple.Create("game.json",game)})using(var w=new StreamWriter(archive.CreateEntry(pair.Item1).Open(),new UTF8Encoding(false)))w.Write(Files.Json.Serialize(pair.Item2));}LanguagePacks.Import(bundle);Require(Loc.T("Görüntü")=="Tela"&&Files.Hash(installed)==hash,"combined ZIP component import");
        var legacy=ui2;legacy.entries.RemoveAll(e=>e.source=="Crisp interface"||e.source.StartsWith("Keeps menu, dialogue")||e.source.StartsWith("For high-resolution gameplay")||e.source.StartsWith("Invert weapon / equipment")||e.source.StartsWith("Reverses vertical selection"));Require(legacy.entries.Count==294,"v5.1 fixture size");LanguagePacks.Import(Save("legacy.json",legacy));Require(Loc.T("Görüntü")=="Tela"&&Loc.T("Keskin arayüz")=="Crisp interface","v5.1 language packs remain compatible");
        string broken=Path.Combine(Files.Assets,"Languages","pt-bad");Directory.CreateDirectory(broken);File.WriteAllText(Path.Combine(broken,"launcher.json"),"invalid JSON");Require(Loc.For("Görüntü","pt-bad")=="Display","corrupt installed catalogue falls back to English");
        File.WriteAllText(Path.Combine(Files.Root,"test-result.json"),Files.Json.Serialize(new{passed=true,isolated=true,launcher_unicode=true,game_utf8_dat=true,dynamic_tokens=true,english_fallback=true,game_component_update=true,protected_metadata=true,missing_tokens_rejected=true,duplicate_ids_rejected=true,path_traversal_rejected=true,builtins_preserved=true,cache_reload=true,translator_export=true,combined_zip=true,corrupt_catalogue_fallback=true}),new UTF8Encoding(false));return 0;
    }
}
