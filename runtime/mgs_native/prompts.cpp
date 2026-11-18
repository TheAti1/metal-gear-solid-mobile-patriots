// SPDX-License-Identifier: GPL-2.0-or-later
#include "prompts.h"
#include "patriots_locale.h"
#include "runtime.h"
#include "settings.h"
#include "loader.h"
#include "gles.h"
#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#endif
#include <SDL3/SDL_gamepad.h>
#include <atomic>
#include <array>
#include <cstring>
#include <iostream>
#include <unordered_map>
#include <stdexcept>
#include <algorithm>
#include <chrono>
namespace PatriotsPrompts {
namespace {
std::atomic<int> active{0}, family{1}; // 0 keyboard, 1 controller; Xbox / PlayStation / Nintendo
const char* L(const char* tr,const char* en){return PatriotsLocale::Text(tr,en);}
std::string Key(int vk) {
    switch(vk) { case 32:return "Space";case 38:return L("Yukarı","Up");case 40:return L("Aşağı","Down");case 37:return L("Sol","Left");case 39:return L("Sağ","Right"); }
#ifdef _WIN32
    if(vk>=0xBA&&vk<=0xE2) {
        wchar_t name[64]{};UINT scan=MapVirtualKeyW(vk,MAPVK_VK_TO_VSC);
        if(GetKeyNameTextW(scan<<16,name,64)>0){char out[256]{};WideCharToMultiByte(CP_UTF8,0,name,-1,out,256,nullptr,nullptr);return out;}
    }
#endif
    return VKName(vk);
}
std::string Pad(int b) {
    int f=Settings::Instance().prompt_style; if(!f)f=family.load();
    const char* xbox[]={"A","B","X","Y"}; const char* ps[]={L("Çarpı","Cross"),L("Daire","Circle"),L("Kare","Square"),L("Üçgen","Triangle")}; const char* nin[]={"B","A","Y","X"};
    if(b>=0&&b<4)return f==2?ps[b]:f==3?nin[b]:xbox[b];
    switch(b) { case 4:return f==2?"Share":"Back";case 5:return f==2?"PS":"Guide";
    case 6:return f==2?"Options":f==3?"+":"Start";case 7:return "L3";case 8:return "R3";
    case 9:return f==2?"L1":f==3?"L":"LB";case 10:return f==2?"R1":f==3?"R":"RB";
    case 11:return L("D-pad Yukarı","D-pad Up");case 12:return L("D-pad Aşağı","D-pad Down");case 13:return L("D-pad Sol","D-pad Left");case 14:return L("D-pad Sağ","D-pad Right");
    case 26:return f==2?"L2":f==3?"ZL":"LT";case 27:return f==2?"R2":f==3?"ZR":"RT";
    default:return PadButtonName(b); }
}
void Replace(std::string& s,const std::string& from,const std::string& to) {
    size_t pos=0; while((pos=s.find(from,pos))!=std::string::npos){s.replace(pos,from.size(),to);pos+=to.size();}
}
struct Rule {const char* from; const char* to;};
#include "prompt_rules.inc"
#include "english_prompt_templates.inc"
std::string Template(std::string s) {
    auto exact=kEnglishPrompts.find(s);if(exact!=kEnglishPrompts.end())return exact->second;
    for(const auto& r:kPromptRules) Replace(s,r.from,r.to);
    return s;
}
struct Cached {std::string original,last;uint32_t ptr=0;uint64_t touched=0;};
std::unordered_map<uint32_t,Cached> cache;
uint32_t clones=0; unsigned ring=0;uint64_t clock=0;
void Draw(Cpu* cpu,uint64_t,uint32_t,void*) {
    uint32_t desc=0; std::array<uint32_t,16> w{};cpu->RegRead(Cpu::REG_R1,&desc);
    if(cpu->MemRead(desc,w.data(),64)!=Cpu::OK||!w[3]||w[4]>8192)return;
    char raw[8193]{}; size_t n=std::min<size_t>(w[4],8192);
    if(cpu->MemRead(w[3],raw,n)!=Cpu::OK)return;
    std::string original(raw,n); if(original.find('\0')!=std::string::npos)original.resize(original.find('\0'));
    std::string text=Rewrite(original);
    // The original phone configurator formats values as "Key 2", "A", "B". Match the real row on its
    // own controls page; a bare number or A/B elsewhere (ammo, timers, dialogue) is never rewritten.
    static auto controls=std::chrono::steady_clock::time_point{};static std::string row;static int page=0;
    const auto now=std::chrono::steady_clock::now();
    if(original.size()==3&&original[0]>='1'&&original[0]<='8'&&original.substr(1)=="/8"){controls=now;page=original[0]-'0';}
    if(Settings::Instance().dynamic_prompts&&now-controls<std::chrono::milliseconds(100)) {
        static const std::unordered_map<std::string,std::string> rows={{"Sol","left"},{"Sağ","right"},{"Yukarı","up"},{"Aşağı","down"},{"Eylem","a"},{"Birinci Şahıs","b"},{"Kamera","star"},{"Left","left"},{"Right","right"},{"Up","up"},{"Down","down"},{"Action","a"},{"First Person","b"},{"Camera","star"}};
        if(page>1){
            if(original=="2, 8, 6, 4"||original=="2,4,6,8")text=Label("move");
            else if((original=="A, Orta"||original=="A, Center")){text=Label("a");auto ok=Label("ok");if(ok!=text)text+=" / "+ok;}
            else if(original=="A")text=Label("a");else if(original=="B")text=Label("b");
            else if(original=="*"||(original=="Tuş *"||original=="Key *"))text=Label("star");
        }
        auto r=rows.find(original);if(r==rows.end())for(auto i=rows.begin();i!=rows.end();++i)if(original==PatriotsLocale::Text(i->first.c_str(),i->first.c_str())){r=i;break;}if(r!=rows.end())row=r->second;
        else if(page==1&&!row.empty()&&(w[14]&0xFF)==1&&((original.compare(0,5,"Tuş ")==0||original.compare(0,4,"Key ")==0)||original=="A"||original=="B")){text=Label(row);row.clear();}
    }
    if(text==original||text.size()>8191)return;
    // These are transient draw descriptors. Never modify/free the game's owning string or its rich-text cache.
    // Clearing cached/owning bits makes IwGx recompute glyphs and wrapping from the current input labels.
    if(!clones)clones=S3ERuntime::Instance().GuestAlloc(64*32);
    if(!clones)return;
    auto it=cache.find(w[3]);
    if(it==cache.end()) {
        uint32_t reuse=0;if(cache.size()>=512){auto oldest=cache.begin();for(auto pos=cache.begin();pos!=cache.end();++pos)if(pos->second.touched<oldest->second.touched)oldest=pos;reuse=oldest->second.ptr;cache.erase(oldest);}
        Cached value;value.ptr=reuse;it=cache.emplace(w[3],std::move(value)).first;
    }
    auto& c=it->second;c.touched=++clock;
    if(!c.ptr)c.ptr=S3ERuntime::Instance().GuestAlloc(8192);
    if(!c.ptr)return;
    if(c.last!=text) {
        cpu->MemWrite(c.ptr,text.c_str(),text.size()+1);c.last=text;
        if(getenv("MGS_PROMPT_CAPTURE"))GLES::Instance().RequestScreenshot();
        if(getenv("MGS_TRACE_PROMPTS"))std::cout<<"[Prompt] "<<original<<" => "<<text<<std::endl;
    }
    c.original=original;w[3]=c.ptr;w[4]=static_cast<uint32_t>(text.size());w[5]=0xFFFFFFFF;
    w[8]=0;w[14]&=~3u;
    uint32_t clone=clones+64*(ring++%32);cpu->MemWrite(clone,w.data(),64);cpu->RegWrite(Cpu::REG_R1,&clone);
}
}
void KeyboardInput(int vk) { if(!Settings::Instance().KeysForVK(vk).empty())active=0; }
void PadInput(int type) {
    int f=1;
    if(type==SDL_GAMEPAD_TYPE_PS3||type==SDL_GAMEPAD_TYPE_PS4||type==SDL_GAMEPAD_TYPE_PS5)f=2;
    else if(type==SDL_GAMEPAD_TYPE_NINTENDO_SWITCH_PRO||type==SDL_GAMEPAD_TYPE_NINTENDO_SWITCH_JOYCON_LEFT||type==SDL_GAMEPAD_TYPE_NINTENDO_SWITCH_JOYCON_RIGHT||type==SDL_GAMEPAD_TYPE_NINTENDO_SWITCH_JOYCON_PAIR)f=3;
    family=f;active=1;
}
void PadDisconnected(){active=0;}
std::string Label(const std::string& action) {
    if(action=="move") {
        if(active&&Settings::Instance().left_stick_dpad)return L("[Sol analog]","[Left stick]");
        if(!active){auto kb=Settings::Instance().KeyboardBindings();std::string out="[";for(int i:{0,2,1,3}){if(out!="[")out+="/";out+=kb[i].empty()?"?":Key(kb[i][0]);}return out+"]";}
        return Label("up")+"/"+Label("left")+"/"+Label("down")+"/"+Label("right");
    }
    const auto& actions=PhoneActions();size_t i=0;for(;i<actions.size();++i)if(action==actions[i].id)break;
    if(i==actions.size())return "[?]";
    auto binds=active?Settings::Instance().ControllerBindings():Settings::Instance().KeyboardBindings();
    // The game's OK prompt also accepts its Action/OK key. On controllers this is normally South.
    if(binds[i].empty()&&action=="ok")return Label("a");
    if(binds[i].empty())return L("[Atanmamış]","[Unbound]");
    std::string result="[";int count=0;
    for(int b:binds[i]){if(b<0)continue;if(count++)result+=" / ";result+=active?Pad(b):Key(b);if(count==2)break;}
    return result+"]";
}
std::string Rewrite(const std::string& text) {
    if(text=="Loading...")return L("Yükleniyor...","Loading...");
    // Imported packs contain explicit action tokens, which remain meaningful
    // even when legacy phone-phrase rewriting is disabled in the settings.
    if(!Settings::Instance().dynamic_prompts&&text.find("{pc:")==std::string::npos)return text;
    if(text=="2,4,6,8")return Label("move");
    std::string s=Template(text);
    if(s==text&&s.find("{pc:")==std::string::npos)return text;
    for(const auto& a:PhoneActions())Replace(s,"{pc:"+std::string(a.id)+"}",Label(a.id));
    Replace(s,"{pc:move}",Label("move"));return s;
}
void Init(Cpu* cpu){cache.clear();clones=0;ring=0;cpu->AddCodeHook(Draw,nullptr,G(0x4A07FAE4),G(0x4A07FAE4));}
void SelfTest() {
    auto& s=Settings::Instance();auto savedKb=s.KeyboardBindings(),savedPad=s.ControllerBindings();int savedStyle=s.prompt_style;bool savedDynamic=s.dynamic_prompts;
    auto testKb=savedKb,testPad=savedPad;testKb[4]={32,74};testKb[5]={67};testPad[4]={0,27};testPad[9]={9};s.SetKeyboardBindings(testKb);s.SetControllerBindings(testPad);s.prompt_style=0;s.dynamic_prompts=true;
    KeyboardInput(32);if(Rewrite("Eylem tuşuna bas.").find("[Space")!=0)throw std::runtime_error("keyboard prompt");
    PadInput(SDL_GAMEPAD_TYPE_XBOXONE);if(Rewrite("* tuşuna bas.").find("[LB]")!=0)throw std::runtime_error("Xbox prompt");
    PadInput(SDL_GAMEPAD_TYPE_PS5);if(Rewrite("* tuşuna bas.").find("[L1]")!=0)throw std::runtime_error("PlayStation prompt");
    if(Rewrite("Press * to switch between camera mode and key mode.").find("[L1]")==std::string::npos)throw std::runtime_error("English star prompt");
    if(Rewrite("* Yenilikçi kamuflaj sistemi")!="* Yenilikçi kamuflaj sistemi")throw std::runtime_error("bullet altered");
    if(Rewrite("Loading...")!=L("Yükleniyor...","Loading..."))throw std::runtime_error("loading translation");
    auto tokenized=Rewrite("Use {pc:a} to fire.");if(tokenized.find("{pc:")!=std::string::npos||tokenized=="Use {pc:a} to fire.")throw std::runtime_error("community action token");
    s.dynamic_prompts=false;if(Rewrite("Use {pc:a} to fire.").find("{pc:")!=std::string::npos)throw std::runtime_error("explicit action token with legacy rewrite off");s.dynamic_prompts=true;
    auto kb=s.KeyboardBindings();auto changed=kb;changed[4]={75};s.SetKeyboardBindings(changed);KeyboardInput(75);
    if(Rewrite("Eylem tuşuna bas.").find("[K]")!=0)throw std::runtime_error("live rebind");s.SetKeyboardBindings(kb);active=0;
    if(Rewrite("Press 1 to switch to first person view.").find("[C]")==std::string::npos)throw std::runtime_error("English first person action");
    PadInput(SDL_GAMEPAD_TYPE_PS5);auto ps=Rewrite("Press the Action key to fire.");
    if(ps.find(Settings::Instance().language?"Cross":"Çarpı")==std::string::npos)throw std::runtime_error("localized pad labels");active=0;
    s.SetKeyboardBindings(savedKb);s.SetControllerBindings(savedPad);s.prompt_style=savedStyle;s.dynamic_prompts=savedDynamic;
    std::cout<<"[PatriotsTests] keyboard, Xbox, PS5, live rebind, loading, untouched bullets PASS"<<std::endl;
}
}
