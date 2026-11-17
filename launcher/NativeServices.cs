// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

public static class Files
{
    public static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string Engine=Path.Combine(Root,"Oyun");
    public static readonly string Assets=Path.Combine(Root,"Arayuz");
    public static readonly string Docs=Path.Combine(Root,"Belgeler");
    public static readonly string Preferences=Path.Combine(Assets,"ayarlar.json");
    public static readonly string Ini=Path.Combine(Engine,"mgs_mobile.ini");
    public static readonly string Data=Path.Combine(Engine,"game","data");
    public static readonly string Backups=Path.Combine(Root,"Kayit Yedekleri");
    public static readonly JavaScriptSerializer Json=new JavaScriptSerializer{MaxJsonLength=16*1024*1024};
    public static string Hash(string file){using(var hash=SHA256.Create())using(var stream=File.OpenRead(file))return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
    public static void Atomic(string file,string text){Directory.CreateDirectory(Path.GetDirectoryName(file));string temp=file+".tmp";File.WriteAllText(temp,text,new UTF8Encoding(false));if(File.Exists(file))File.Replace(temp,file,null);else File.Move(temp,file);}
    public static void Log(string text){try{File.AppendAllText(Path.Combine(Engine,"baslatici.log"),DateTime.UtcNow.ToString("o")+" "+text+Environment.NewLine,Encoding.UTF8);}catch{}}
}

public sealed class ActionBinding
{
    public string Id{get;set;} public string Label{get;set;}
    public int Key1{get;set;}public int Key2{get;set;}public int Pad1{get;set;}public int Pad2{get;set;}
}
public static class Inputs
{
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetKeyNameText(int lparam,StringBuilder text,int length);
    [DllImport("user32.dll")]private static extern uint MapVirtualKey(uint code,uint mode);
    public static string KeyName(int key){if(key<=0)return "—";if(key>=48&&key<=90)return ((char)key).ToString();switch(key){case 13:return "Enter";case 32:return "Space";case 9:return "Tab";case 27:return "Esc";case 8:return "Backspace";case 16:return "Shift";case 17:return "Ctrl";case 18:return "Alt";case 37:return "←";case 38:return "↑";case 39:return "→";case 40:return "↓";case 106:return "Num *";case 107:return "Num +";case 109:return "Num −";case 111:return "Num /";}
        if(key>=112&&key<=135)return "F"+(key-111);if(key>=96&&key<=105)return "Num "+(key-96);var s=new StringBuilder(64);if(GetKeyNameText((int)MapVirtualKey((uint)key,0)<<16,s,64)>0)return s.ToString();return "VK "+key;}
    public static readonly Dictionary<int,string> Pads=new Dictionary<int,string>{{-1,"—"},{0,"A / Çarpı"},{1,"B / Daire"},{2,"X / Kare"},{3,"Y / Üçgen"},{6,"Start / Options"},{7,"Sol çubuk tıklama"},{9,"LB / L1"},{10,"RB / R1"},{11,"D-pad ↑"},{12,"D-pad ↓"},{13,"D-pad ←"},{14,"D-pad →"},{26,"LT / L2"},{27,"RT / R2"},{5,"Guide / Home"},{15,"Ek düğme 1"},{16,"Sağ palet 1"},{17,"Sol palet 1"},{18,"Sağ palet 2"},{19,"Sol palet 2"},{20,"Dokunmatik yüzey"},{21,"Ek düğme 2"},{22,"Ek düğme 3"},{23,"Ek düğme 4"},{24,"Ek düğme 5"},{25,"Ek düğme 6"}};
    public static string PadName(int id){return Loc.T(Pads.ContainsKey(id)?Pads[id]:"?");}
    public static List<ActionBinding> Defaults(bool classic=false)
    {
        var b=new List<ActionBinding>{
            new ActionBinding{Id="up",Label="İleri / yukarı",Key1=87,Key2=38,Pad1=11,Pad2=-1},
            new ActionBinding{Id="down",Label="Geri / aşağı",Key1=83,Key2=40,Pad1=12,Pad2=-1},
            new ActionBinding{Id="left",Label="Sola hareket",Key1=65,Key2=37,Pad1=13,Pad2=-1},
            new ActionBinding{Id="right",Label="Sağa hareket",Key1=68,Key2=39,Pad1=14,Pad2=-1},
            new ActionBinding{Id="a",Label="Eylem / ateş / onay",Key1=32,Key2=74,Pad1=0,Pad2=27},
            new ActionBinding{Id="b",Label="Birinci şahıs görünümü",Key1=67,Key2=0,Pad1=3,Pad2=26},
            new ActionBinding{Id="ok",Label="Merkez / onay",Key1=13,Key2=0,Pad1=-1,Pad2=-1},
            new ActionBinding{Id="lsk",Label="Duraklatma menüsü",Key1=81,Key2=9,Pad1=6,Pad2=2},
            new ActionBinding{Id="rsk",Label="Ekipman / geri",Key1=69,Key2=27,Pad1=1,Pad2=10},
            new ActionBinding{Id="star",Label="Kamera / telefon *",Key1=82,Key2=106,Pad1=9,Pad2=-1},
            new ActionBinding{Id="hash",Label="Telefon #",Key1=70,Key2=0,Pad1=7,Pad2=-1},
            new ActionBinding{Id="clear",Label="Sil / temizle",Key1=8,Key2=0,Pad1=-1,Pad2=-1},
        };
        for(int i=0;i<=9;i++)b.Add(new ActionBinding{Id="key"+i,Label="Telefon tuşu "+i,Key1=48+i,Key2=96+i,Pad1=-1,Pad2=-1});
        b.Add(new ActionBinding{Id="volup",Label="Oyun sesini artır",Key1=33,Pad1=-1,Pad2=-1});b.Add(new ActionBinding{Id="voldown",Label="Oyun sesini azalt",Key1=34,Pad1=-1,Pad2=-1});
        if(classic){foreach(var item in b.Take(4))item.Key1=item.Key2;x(b,"up").Key2=0;x(b,"down").Key2=0;x(b,"left").Key2=0;x(b,"right").Key2=0;x(b,"a").Key1=90;x(b,"a").Key2=0;x(b,"lsk").Key1=65;x(b,"lsk").Key2=0;x(b,"rsk").Key1=88;x(b,"rsk").Key2=27;x(b,"ok").Key2=32;}
        return b;
    }
    private static ActionBinding x(List<ActionBinding> list,string id){return list.First(b=>b.Id==id);}
}

public sealed class Preferences
{
    public string Language{get;set;}
    public bool DynamicPrompts{get;set;}public int PromptStyle{get;set;}public bool DiscordPresence{get;set;}public bool DiscordRoom{get;set;}
    public int Version{get;set;}public bool Fullscreen{get;set;}public int Monitor{get;set;}public int Width{get;set;}public int Height{get;set;}
    public bool Wide{get;set;}public int Fov{get;set;}public bool Smooth{get;set;}public int Msaa{get;set;}public int Scale{get;set;}public bool PixelMode{get;set;}public bool Software{get;set;}public bool DrawAll{get;set;}
    public bool Analog{get;set;}public bool Diagonal{get;set;}public bool Stick{get;set;}public bool SkipIntro{get;set;}public bool PauseUnfocused{get;set;}public int SoftKeys{get;set;}
    public int Volume{get;set;}public bool Mute{get;set;}public bool ShowBrand{get;set;}public bool HideLauncher{get;set;}public bool AutoBackup{get;set;}public bool DirectPlay{get;set;}
    public long PlaySeconds{get;set;}public string LastPlayed{get;set;}public List<ActionBinding> Bindings{get;set;}
    public Preferences(){Version=5;Language="tr";DynamicPrompts=true;DiscordPresence=true;DiscordRoom=true;Fullscreen=true;Width=1280;Height=720;Wide=true;Fov=100;Msaa=2;Scale=1;Stick=true;SkipIntro=true;PauseUnfocused=true;SoftKeys=1;Volume=85;ShowBrand=true;HideLauncher=true;AutoBackup=true;Bindings=Inputs.Defaults();LastPlayed="";}
    public void Validate(){Language=Loc.Normalize(Language);PromptStyle=Math.Max(0,Math.Min(3,PromptStyle));Width=Math.Max(640,Math.Min(Width,7680));Height=Math.Max(480,Math.Min(Height,4320));Monitor=Math.Max(0,Math.Min(Monitor,15));Fov=Math.Max(50,Math.Min(Fov,200));Msaa=new[]{0,2,4,8}.Contains(Msaa)?Msaa:0;Scale=Math.Max(1,Math.Min(Scale,3));Volume=Math.Max(0,Math.Min(Volume,100));SoftKeys=Math.Max(0,Math.Min(SoftKeys,2));
        var supplied=Bindings??new List<ActionBinding>();var valid=Inputs.Defaults();foreach(var b in valid){var old=supplied.FirstOrDefault(x=>x!=null&&x.Id==b.Id);if(old==null)continue;b.Key1=ValidKey(old.Key1)?old.Key1:0;b.Key2=ValidKey(old.Key2)?old.Key2:0;b.Pad1=Inputs.Pads.ContainsKey(old.Pad1)?old.Pad1:-1;b.Pad2=Inputs.Pads.ContainsKey(old.Pad2)?old.Pad2:-1;}Bindings=valid;
    }
    private static bool ValidKey(int key){return key>=0&&key<256&&key!=112&&key!=116&&key!=123;}
    public static Preferences Load(){Preferences p;try{p=Files.Json.Deserialize<Preferences>(File.ReadAllText(Files.Preferences));p.Validate();}catch{p=new Preferences();}try{NativeConfig.Read(p);}catch{}return p;}
    public void Save(){Validate();Files.Atomic(Files.Preferences,Files.Json.Serialize(this));}
}

public static class NativeConfig
{
    public static Dictionary<string,string> Values(){var values=new Dictionary<string,string>();if(!File.Exists(Files.Ini))return values;foreach(string line in File.ReadAllLines(Files.Ini)){string clean=line.Trim();if(clean.StartsWith("#")||clean.StartsWith(";")||clean.StartsWith("["))continue;int eq=clean.IndexOf('=');if(eq<1)continue;string value=clean.Substring(eq+1).Split('#')[0].Trim();values[clean.Substring(0,eq).Trim()]=value;}return values;}
    private static int Num(Dictionary<string,string>d,string key,int fallback){int value;return d.ContainsKey(key)&&Int32.TryParse(d[key],out value)?value:fallback;}
    public static void Read(Preferences s){var d=Values();if(d.Count==0)return;s.DynamicPrompts=Num(d,"dynamic_prompts",s.DynamicPrompts?1:0)!=0;s.PromptStyle=Num(d,"prompt_style",s.PromptStyle);s.DiscordPresence=Num(d,"discord_presence",s.DiscordPresence?1:0)!=0;s.DiscordRoom=Num(d,"discord_room",s.DiscordRoom?1:0)!=0;s.Fullscreen=Num(d,"fullscreen",s.Fullscreen?1:0)!=0;s.Wide=Num(d,"arbitrary_resolution",s.Wide?1:0)!=0;s.Fov=Num(d,"fov_percent",s.Fov);s.Smooth=Num(d,"texture_filtering",s.Smooth?1:0)!=0;s.Msaa=Num(d,"msaa",s.Msaa);s.Scale=Num(d,"supersample",s.Scale);s.PixelMode=Num(d,"native_resolution",s.PixelMode?1:0)!=0;s.Software=Num(d,"software_renderer",s.Software?1:0)!=0;s.DrawAll=Num(d,"draw_all_geometry",s.DrawAll?1:0)!=0;s.Analog=Num(d,"analog_movement",s.Analog?1:0)!=0;s.Diagonal=Num(d,"diagonal_movement",s.Diagonal?1:0)!=0;s.Stick=Num(d,"left_stick_dpad",s.Stick?1:0)!=0;s.SkipIntro=Num(d,"skip_intro",s.SkipIntro?1:0)!=0;s.PauseUnfocused=Num(d,"pause_on_focus_loss",s.PauseUnfocused?1:0)!=0;s.SoftKeys=Num(d,"hide_soft_keys",s.SoftKeys);if(!s.Fullscreen){s.Width=Num(d,"window_w",s.Width);s.Height=Num(d,"window_h",s.Height);}
        foreach(var b in s.Bindings){if(d.ContainsKey("kb."+b.Id)){int[] keys=List(d["kb."+b.Id]);b.Key1=keys.Length>0?keys[0]:0;b.Key2=keys.Length>1?keys[1]:0;}if(d.ContainsKey("pad."+b.Id)){int[] keys=List(d["pad."+b.Id]);b.Pad1=keys.Length>0?keys[0]:-1;b.Pad2=keys.Length>1?keys[1]:-1;}}s.Validate();
    }
    private static int[] List(string value){return value.Split(',').Where(x=>!String.IsNullOrWhiteSpace(x)).Select(x=>{int n;return Int32.TryParse(x,out n)?n:-1;}).ToArray();}
    private static string Bit(bool b){return b?"1":"0";}
    public static void WriteLiveControls(Preferences s){s.Validate();var t=new StringBuilder();t.Append("dynamic_prompts=").Append(Bit(s.DynamicPrompts)).Append("\nprompt_style=").Append(s.PromptStyle).Append("\ndiscord_presence=").Append(Bit(s.DiscordPresence)).Append("\ndiscord_room=").Append(Bit(s.DiscordRoom)).Append('\n');foreach(var b in s.Bindings){t.Append("kb.").Append(b.Id).Append('=').Append(String.Join(",",new[]{b.Key1,b.Key2}.Where(k=>k>0).Distinct())).Append('\n');t.Append("pad.").Append(b.Id).Append('=').Append(String.Join(",",new[]{b.Pad1,b.Pad2}.Where(k=>k>=0).Distinct())).Append('\n');}Files.Atomic(Path.Combine(Files.Engine,"patriots_live.ini"),t.ToString());}
    public static void Write(Preferences s){s.Validate();s.Save();var d=Values();var changes=new Dictionary<string,string>{{"language",Loc.Normalize(s.Language)},{"dynamic_prompts",Bit(s.DynamicPrompts)},{"prompt_style",s.PromptStyle.ToString()},{"discord_presence",Bit(s.DiscordPresence)},{"discord_room",Bit(s.DiscordRoom)},{"fullscreen",Bit(s.Fullscreen)},{"arbitrary_resolution",Bit(s.Wide)},{"texture_filtering",Bit(s.Smooth)},{"custom_textures","0"},{"draw_all_geometry",Bit(s.DrawAll)},{"software_renderer",Bit(s.Software)},{"msaa",s.Msaa.ToString()},{"supersample",s.Scale.ToString()},{"native_resolution",Bit(s.PixelMode)},{"hide_soft_keys",s.SoftKeys.ToString()},{"fov_percent",s.Fov.ToString()},{"left_stick_dpad",Bit(s.Stick)},{"analog_movement",Bit(s.Analog)},{"diagonal_movement",Bit(s.Diagonal)},{"skip_intro",Bit(s.SkipIntro)},{"pause_on_focus_loss",Bit(s.PauseUnfocused)},{"debug_mode","0"},{"cheat_infinite_health","0"},{"cheat_infinite_ammo","0"},{"touch_mode","0"},{"window_saved","1"},{"window_w",s.Width.ToString()},{"window_h",s.Height.ToString()}};
        var screen=System.Windows.Forms.Screen.AllScreens[Math.Min(s.Monitor,System.Windows.Forms.Screen.AllScreens.Length-1)].WorkingArea;changes["window_x"]=(screen.X+Math.Max(0,(screen.Width-s.Width)/2)).ToString();changes["window_y"]=(screen.Y+Math.Max(0,(screen.Height-s.Height)/2)).ToString();
        foreach(var b in s.Bindings){changes["kb."+b.Id]=String.Join(",",new[]{b.Key1,b.Key2}.Where(k=>k>0).Distinct());changes["pad."+b.Id]=String.Join(",",new[]{b.Pad1,b.Pad2}.Where(k=>k>=0).Distinct());}
        foreach(var pair in changes)d[pair.Key]=pair.Value;var text=new StringBuilder("# Patriots Ceviri - MGS Mobile\n");foreach(var pair in d)text.Append(pair.Key).Append('=').Append(pair.Value).Append('\n');Files.Atomic(Files.Ini,text.ToString());
    }
}

public sealed class PadProbe : IDisposable
{
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]private static extern bool SetDllDirectory(string path);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]private static extern bool SDL_InitSubSystem(uint flags);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)]private static extern void SDL_SetMainReady();
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)]private static extern IntPtr SDL_GetError();
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)]private static extern void SDL_QuitSubSystem(uint flags);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)]private static extern IntPtr SDL_GetGamepads(out int count);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)]private static extern void SDL_free(IntPtr pointer);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)]private static extern IntPtr SDL_GetGamepadNameForID(uint id);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)]private static extern IntPtr SDL_OpenGamepad(uint id);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)]private static extern void SDL_CloseGamepad(IntPtr handle);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)]private static extern void SDL_UpdateGamepads();
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]private static extern bool SDL_GamepadConnected(IntPtr handle);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]private static extern bool SDL_GetGamepadButton(IntPtr handle,int button);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)]private static extern short SDL_GetGamepadAxis(IntPtr handle,int axis);
    [DllImport("SDL3.dll",CallingConvention=CallingConvention.Cdecl)][return:MarshalAs(UnmanagedType.I1)]private static extern bool SDL_RumbleGamepad(IntPtr handle,ushort low,ushort high,uint ms);
    private bool ready;private IntPtr handle;private uint opened;
    public string Error{get;private set;}public bool Ready{get{return ready;}}public float[] Axes{get;private set;}public List<int> Pressed{get;private set;}
    public PadProbe(){Axes=new float[6];Pressed=new List<int>();try{SetDllDirectory(Files.Engine);SDL_SetMainReady();ready=SDL_InitSubSystem(0x2000);if(!ready)Error=Marshal.PtrToStringAnsi(SDL_GetError());}catch(Exception e){Error=e.Message;}}
    public Dictionary<uint,string> Devices(){var d=new Dictionary<uint,string>();if(!ready)return d;IntPtr ids=IntPtr.Zero;try{SDL_UpdateGamepads();int count;ids=SDL_GetGamepads(out count);for(int i=0;i<count;i++){uint id=(uint)Marshal.ReadInt32(ids,i*4);d[id]=Marshal.PtrToStringAnsi(SDL_GetGamepadNameForID(id))??Loc.T("Kontrolcü");}}catch(Exception e){Error=e.Message;}finally{if(ids!=IntPtr.Zero)SDL_free(ids);}return d;}
    public bool Poll(uint id){if(!ready)return false;try{SDL_UpdateGamepads();if(handle!=IntPtr.Zero&&(opened!=id||!SDL_GamepadConnected(handle))){SDL_CloseGamepad(handle);handle=IntPtr.Zero;}if(handle==IntPtr.Zero&&id!=0){handle=SDL_OpenGamepad(id);opened=id;}Pressed.Clear();if(handle==IntPtr.Zero){Array.Clear(Axes,0,6);return false;}for(int i=0;i<26;i++)if(SDL_GetGamepadButton(handle,i))Pressed.Add(i);for(int i=0;i<6;i++)Axes[i]=SDL_GetGamepadAxis(handle,i)/32767f;if(Axes[4]>.5f)Pressed.Add(26);if(Axes[5]>.5f)Pressed.Add(27);return true;}catch(Exception e){Error=e.Message;return false;}}
    public bool Rumble(){try{return handle!=IntPtr.Zero&&SDL_RumbleGamepad(handle,22000,30000,350);}catch{return false;}}
    public void Dispose(){if(handle!=IntPtr.Zero){SDL_CloseGamepad(handle);handle=IntPtr.Zero;}if(ready){SDL_QuitSubSystem(0x2000);ready=false;}}
}

public static class Saves
{
    private static bool Allowed(string name){return Path.GetFileName(name)==name&&(name.Equals("savedata.i3d",StringComparison.OrdinalIgnoreCase)||name.Equals("savedata2.i3d",StringComparison.OrdinalIgnoreCase));}
    public static string Create(string data=null,string backups=null){data=data??Files.Data;backups=backups??Files.Backups;var files=Directory.GetFiles(data).Where(p=>Allowed(Path.GetFileName(p))).ToArray();if(files.Length==0)return null;Directory.CreateDirectory(backups);string name=Path.Combine(backups,"MGS_"+DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")+".zip");using(var zip=ZipFile.Open(name,ZipArchiveMode.Create)){foreach(string file in files)zip.CreateEntryFromFile(file,Path.GetFileName(file));using(var writer=new StreamWriter(zip.CreateEntry("patriots_mgs_save.json").Open()))writer.Write("{\"version\":1,\"game\":\"mgsm\"}");}return name;}
    public static int Restore(string file,string data=null){data=data??Files.Data;using(var zip=ZipFile.OpenRead(file)){if(zip.GetEntry("patriots_mgs_save.json")==null)throw new InvalidDataException(Loc.T("Bu dosya MGS Mobile kayıt yedeği değil."));var entries=zip.Entries.Where(x=>x.FullName!="patriots_mgs_save.json").ToArray();foreach(var entry in entries)if(!Allowed(entry.FullName)||entry.Length>8*1024*1024)throw new InvalidDataException(Loc.T("Yedek dosyasında beklenmeyen içerik var."));foreach(var entry in entries)entry.ExtractToFile(Path.Combine(data,entry.FullName),true);return entries.Length;}}
}

// Windows per-session audio. This changes only the owned game process, never the device master volume.
public static class SessionAudio
{
    [ComImport,Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]private class DeviceEnumerator{}
    [ComImport,Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]private interface IDevices{[PreserveSig]int EnumEndpoints(int flow,int mask,out IntPtr devices);[PreserveSig]int DefaultEndpoint(int flow,int role,out IDevice device);[PreserveSig]int GetDevice([MarshalAs(UnmanagedType.LPWStr)]string id,out IDevice device);[PreserveSig]int Register(IntPtr cb);[PreserveSig]int Unregister(IntPtr cb);}
    [ComImport,Guid("D666063F-1587-4E43-81F1-B948E807363F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]private interface IDevice{[PreserveSig]int Activate(ref Guid iid,int context,IntPtr activation,[MarshalAs(UnmanagedType.IUnknown)]out object instance);[PreserveSig]int OpenStore(int mode,out IntPtr store);[PreserveSig]int GetId(out IntPtr id);[PreserveSig]int GetState(out int state);}
    [ComImport,Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]private interface IManager{[PreserveSig]int GetControl(ref Guid session,uint flags,out IntPtr control);[PreserveSig]int GetVolume(ref Guid session,uint flags,out IntPtr volume);[PreserveSig]int Enumerator(out IEnumeration sessions);[PreserveSig]int Register(IntPtr cb);[PreserveSig]int Unregister(IntPtr cb);[PreserveSig]int Duck([MarshalAs(UnmanagedType.LPWStr)]string id,IntPtr cb);[PreserveSig]int Unduck(IntPtr cb);}
    [ComImport,Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]private interface IEnumeration{[PreserveSig]int Count(out int count);[PreserveSig]int Session(int index,[MarshalAs(UnmanagedType.IUnknown)]out object session);}
    [ComImport,Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]private interface IControl{[PreserveSig]int State(out int state);[PreserveSig]int Display(out IntPtr name);[PreserveSig]int SetDisplay(IntPtr name,ref Guid context);[PreserveSig]int Icon(out IntPtr name);[PreserveSig]int SetIcon(IntPtr name,ref Guid context);[PreserveSig]int Group(out Guid id);[PreserveSig]int SetGroup(ref Guid id,ref Guid context);[PreserveSig]int Register(IntPtr cb);[PreserveSig]int Unregister(IntPtr cb);[PreserveSig]int Identifier(out IntPtr id);[PreserveSig]int Instance(out IntPtr id);[PreserveSig]int Process(out uint id);[PreserveSig]int SystemSounds();[PreserveSig]int Duck([MarshalAs(UnmanagedType.Bool)]bool optOut);}
    [ComImport,Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]private interface IVolume{[PreserveSig]int Set(float volume,ref Guid context);[PreserveSig]int Get(out float volume);[PreserveSig]int Mute([MarshalAs(UnmanagedType.Bool)]bool muted,ref Guid context);[PreserveSig]int GetMute([MarshalAs(UnmanagedType.Bool)]out bool muted);}
    public static bool Apply(int pid,int level,bool muted){object enumerator=null,instance=null;IDevice device=null;IEnumeration sessions=null;try{enumerator=new DeviceEnumerator();if(((IDevices)enumerator).DefaultEndpoint(0,1,out device)!=0)return false;Guid iid=typeof(IManager).GUID;if(device.Activate(ref iid,23,IntPtr.Zero,out instance)!=0)return false;if(((IManager)instance).Enumerator(out sessions)!=0)return false;int count;sessions.Count(out count);for(int i=0;i<count;i++){object session=null;try{sessions.Session(i,out session);var control=session as IControl;if(control==null)continue;uint process;control.Process(out process);if(process!=(uint)pid)continue;var volume=session as IVolume;if(volume==null)continue;Guid context=Guid.Empty;float wanted=Math.Max(0,Math.Min(level,100))/100f,actual;bool actualMute;if(volume.Set(wanted,ref context)!=0||volume.Mute(muted,ref context)!=0)return false;return volume.Get(out actual)==0&&Math.Abs(actual-wanted)<.01f&&volume.GetMute(out actualMute)==0&&actualMute==muted;}finally{if(session!=null)Marshal.ReleaseComObject(session);}}return false;}catch{return false;}finally{if(sessions!=null)Marshal.ReleaseComObject(sessions);if(instance!=null)Marshal.ReleaseComObject(instance);if(device!=null)Marshal.ReleaseComObject(device);if(enumerator!=null)Marshal.ReleaseComObject(enumerator);}}
}

public static class Game
{
    [DllImport("user32.dll")]private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern bool SetWindowText(IntPtr window,string text);
    [DllImport("user32.dll")]private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int w,int h,uint flags);
    public static volatile int Pid;
    public static void Validate(){foreach(string file in new[]{"mgs_mobile.exe","SDL3.dll","unicorn.dll","game/mgs_ngi","game/data/settings.group.bin","game/data/common/fixed.group.bin","game/data/localisation/TextGlobal/english.dat"})if(!File.Exists(Path.Combine(Files.Engine,file.Replace('/',Path.DirectorySeparatorChar))))throw new FileNotFoundException(Loc.T("Oyun dosyası eksik: ")+file);}
    public static void Focus(){if(Pid==0)return;try{using(var p=Process.GetProcessById(Pid))SetForegroundWindow(p.MainWindowHandle);}catch{}}
    private static void EnsureSingleGame(){foreach(var process in Process.GetProcesses())using(process){bool matches=false;try{if(!process.ProcessName.StartsWith("mgs_mobile",StringComparison.OrdinalIgnoreCase))continue;string path=process.MainModule.FileName;matches=String.Equals(Path.GetDirectoryName(path).TrimEnd(Path.DirectorySeparatorChar),Files.Engine.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase);}catch{}if(matches)throw new InvalidOperationException(Loc.T("Oyun zaten açık. Yeni sürümle oynamak için önce açık oyunu kapat."));}}

    public static int Run(Preferences settings,bool test=false){EnsureSingleGame();Loc.Set(settings.Language);GameLanguages.Validate(settings.Language);Validate();NativeConfig.Write(settings);if(File.Exists(Path.Combine(Files.Engine,"patriots_live.ini")))File.Delete(Path.Combine(Files.Engine,"patriots_live.ini"));if(settings.AutoBackup){try{string backup=Saves.Create();if(backup!=null)Files.Log("BACKUP "+backup);}catch(Exception e){Files.Log("BACKUP_ERROR "+e.Message);}}
        var start=new ProcessStartInfo(Path.Combine(Files.Engine,"mgs_mobile.exe")){WorkingDirectory=Files.Engine,UseShellExecute=false,CreateNoWindow=true};if(test){if(!start.EnvironmentVariables.ContainsKey("MGS_AUTOTEST"))start.EnvironmentVariables["MGS_AUTOTEST"]="24,1,1000";if(!start.EnvironmentVariables.ContainsKey("MGS_KEY_SCRIPT"))start.EnvironmentVariables["MGS_KEY_SCRIPT"]="0:0D,1:0D,2:0D,3:0D,4:0D,5:0D,10:0D,11:0D,12:0D,13:0D,14:0D,15:0D";start.EnvironmentVariables["MGS_TRACE_VIEW"]="1";}
        Stopwatch timer=Stopwatch.StartNew();using(var process=Process.Start(start))using(var discord=new DiscordPresence(process.Id,settings.Language)){Pid=process.Id;Files.Log("START pid="+Pid+" fullscreen="+settings.Fullscreen+" wide="+settings.Wide);bool volumeSet=false;int ticks=0;try{while(!process.WaitForExit(750)){process.Refresh();IntPtr window=process.MainWindowHandle;if(window!=IntPtr.Zero){SetWindowText(window,"Metal Gear Solid Mobile · Patriots Çeviri");if(ticks<6&&settings.Monitor>0){var b=System.Windows.Forms.Screen.AllScreens[Math.Min(settings.Monitor,System.Windows.Forms.Screen.AllScreens.Length-1)].Bounds;SetWindowPos(window,IntPtr.Zero,b.X,b.Y,settings.Fullscreen?b.Width:settings.Width,settings.Fullscreen?b.Height:settings.Height,4);}}if(ticks++%2==0){var current=Preferences.Load();if(SessionAudio.Apply(Pid,current.Volume,current.Mute)&&!volumeSet){Files.Log("AUDIO_SESSION_OK volume="+current.Volume+" muted="+current.Mute);volumeSet=true;}}if(test&&timer.Elapsed.TotalSeconds>65){process.CloseMainWindow();if(!process.WaitForExit(5000))process.Kill();throw new Exception(Loc.T("Otomatik oyun testi zaman aşımına uğradı."));}}
            Files.Log("EXIT "+process.ExitCode);if(!test){var current=Preferences.Load();current.PlaySeconds+=(long)timer.Elapsed.TotalSeconds;current.LastPlayed=DateTime.Now.ToString("s");current.Save();}return process.ExitCode;
        }finally{Pid=0;}}
    }
    public static List<string> Verify(Action<int,int> progress=null){string file=Path.Combine(Files.Docs,"dosya_butunlugu.json");var expected=Files.Json.Deserialize<Dictionary<string,string>>(File.ReadAllText(file));var errors=new List<string>();int n=0;foreach(var pair in expected){string target=Path.GetFullPath(Path.Combine(Files.Root,pair.Key.Replace('/',Path.DirectorySeparatorChar)));if(!target.StartsWith(Files.Root,StringComparison.OrdinalIgnoreCase)||!File.Exists(target)||Files.Hash(target)!=pair.Value)errors.Add(pair.Key);n++;if(progress!=null&&(n%16==0||n==expected.Count))progress(n,expected.Count);}return errors;}
}
