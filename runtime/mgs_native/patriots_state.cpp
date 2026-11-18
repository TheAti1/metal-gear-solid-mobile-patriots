// SPDX-License-Identifier: GPL-2.0-or-later
#include "patriots_state.h"
#include "patriots_locale.h"
#include "runtime.h"
#include "settings.h"
#include "loader.h"
#include "platform.h"
#include "overlay.h"
#include "vfs.h"
#include <fstream>
#include <iostream>
#include <chrono>
#include <map>
#include <sstream>
#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#endif
namespace PatriotsState {
namespace {
using Clock=std::chrono::steady_clock;
std::string room,roomTitle; bool pending=false,cutscene=false;
const std::map<std::string,std::string> names={
#include "room_names.inc"
};
const std::map<std::string,std::string> namesEn={
#include "room_names_en.inc"
};
const char* L(const char* tr,const char* en){return PatriotsLocale::Text(tr,en);}
std::string Q(const std::string& s) {
    std::string o="\"";for(unsigned char c:s){if(c=='"'||c=='\\')o+='\\';if(c=='\n'){o+="\\n";continue;}if(c=='\r')continue;if(c>=32)o+=c;}return o+'"';
}
std::string RoomName(const std::string& s) {
    for(const auto& n:(Settings::Instance().language?namesEn:names))if(s==n.first||s.compare(0,n.first.size()+1,n.first+"_")==0)return L(n.second.c_str(),n.second.c_str());
    std::string result=s;for(char&c:result)if(c=='_')c=' ';return result;
}
uint32_t GameHash(const char* s){uint32_t h=5381;for(;*s;++s){uint8_t c=static_cast<uint8_t>(*s);if(c-'A'<26u)c+=32;h=h*33+c;}return h;}
uint32_t StateHash(Cpu* cpu) {
    if(!cpu)return 0;uint32_t mgr=0,arr=0,count=0,obj=0,hash=0;
    if(cpu->MemRead(G(0x4A0F0558),&mgr,4)||!mgr)return 0;
    cpu->MemRead(mgr+0x14,&arr,4);cpu->MemRead(mgr+0x18,&count,4);
    if(!arr||!count||count>32)return 0;cpu->MemRead(arr+4*(count-1),&obj,4);if(obj)cpu->MemRead(obj+4,&hash,4);return hash;
}
}
void Tick(bool gameplay) {
    static auto next=Clock::time_point{};if(Clock::now()<next)return;next=Clock::now()+std::chrono::milliseconds(500);
    // Only the launcher writes this small atomic command file. F1 changes remain authoritative until a new command.
    static std::filesystem::file_time_type liveTime{};std::error_code ec;
    auto live=Platform::ExeDir()/"patriots_live.ini";auto stamp=std::filesystem::last_write_time(live,ec);
    if(!ec&&stamp!=liveTime){liveTime=stamp;auto& settings=Settings::Instance();auto kb=settings.KeyboardBindings(),pad=settings.ControllerBindings();
        std::ifstream f(live);std::string line;while(std::getline(f,line)){size_t eq=line.find('=');if(eq==std::string::npos)continue;auto k=line.substr(0,eq),v=line.substr(eq+1);int n=atoi(v.c_str());
            if(k=="dynamic_prompts")settings.dynamic_prompts=n!=0;else if(k=="prompt_style")settings.prompt_style=(std::max)(0,(std::min)(3,n));
            else if(k=="discord_presence")settings.discord_presence=n!=0;else if(k=="discord_room")settings.discord_room=n!=0;
            else if(k=="invert_inventory_scroll")settings.invert_inventory_scroll=n!=0;
            else for(size_t i=0;i<PhoneActions().size();i++){bool isKb=k=="kb."+std::string(PhoneActions()[i].id),isPad=k=="pad."+std::string(PhoneActions()[i].id);if(!isKb&&!isPad)continue;
                std::vector<int> keys;std::stringstream values(v);std::string item;while(std::getline(values,item,','))if(!item.empty()){int key=atoi(item.c_str());if(isKb?(key>0&&key<256):(key>=0&&key<28&&key!=4&&key!=8))keys.push_back(key);} (isKb?kb:pad)[i]=keys;
            }
        }
        settings.SetKeyboardBindings(kb);settings.SetControllerBindings(pad);settings.Save();std::cout<<"[PatriotsControls] Live bindings applied"<<std::endl;
    }
    auto& rt=S3ERuntime::Instance();auto* cpu=rt.GuestCpu();uint32_t hash=StateHash(cpu);
    bool inWorld=gameplay;std::string state=L("Ana menü","Main menu");
    auto is=[&](const char* n){return hash==GameHash(n);};
    if(gameplay)state=cutscene?L("Ara sahne","Cutscene"):L("Görevde","On a mission");
    else if(is("CGameStateCodec")){state=L("Codec görüşmesi","Codec conversation");inWorld=true;}
    else if(is("CGameStatePause")){state=L("Duraklatıldı","Paused");inWorld=true;}
    else if(is("CGameStateInventory")){state=L("Ekipman seçiliyor","Selecting equipment");inWorld=true;}
    else if(is("CGameStateGameOver")){state=L("Görev başarısız","Mission failed");inWorld=true;}
    else if(is("CGameStateSplashScreens")||!hash)state=L("Yükleniyor...","Loading...");
    else if(is("CGameStateCredits"))state=L("Jenerik","Credits");
    if(rt.FocusPaused()||Overlay::Instance().IsOpen())state=L("Duraklatıldı","Paused");
    if(gameplay&&!pending&&!rt.FocusPaused()&&!Overlay::Instance().IsOpen()) {
        pending=true;LuaChunk read;read.ReturnField("ox",1).ReturnGlobal("iw");
        rt.RunLua(read,[](const std::vector<LuaValue>& values){pending=false;if(values.size()>0&&values[0].type==LuaValue::String){room=values[0].s;roomTitle=RoomName(room);}cutscene=values.size()>1&&values[1].Truthy();});
    }
    const std::string shown=inWorld&&Settings::Instance().discord_room?roomTitle:"";
    static std::string last;
    std::string body="{\"pid\":"+std::to_string(GetCurrentProcessId())+",\"language\":"+Q(Settings::Instance().LocaleId())+",\"state\":"+Q(state)+",\"room\":"+Q(shown)+",\"room_id\":"+Q(inWorld?room:"")+",\"state_hash\":"+std::to_string(hash)+"}";
    auto path=Platform::ExeDir()/"patriots_state.json",temp=Platform::ExeDir()/"patriots_state.tmp";
    {std::ofstream f(temp,std::ios::binary|std::ios::trunc);f<<body;}
    MoveFileExW(temp.c_str(),path.c_str(),MOVEFILE_REPLACE_EXISTING|MOVEFILE_WRITE_THROUGH);
    if(body!=last){if(getenv("MGS_TRACE_PRESENCE"))std::cout<<"[PresenceState] "<<body<<std::endl;last=body;}
}
}
