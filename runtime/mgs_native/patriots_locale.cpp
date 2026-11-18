// SPDX-License-Identifier: GPL-2.0-or-later
#include "patriots_locale.h"
#include "settings.h"
#include "platform.h"
#include <fstream>
#include <unordered_map>
#include <mutex>
#include <cstring>
#include <iostream>
namespace PatriotsLocale {
namespace {
std::once_flag once;
std::unordered_map<std::string,std::string> strings;
bool Number(std::ifstream& file,uint32_t& value){unsigned char b[4];if(!file.read(reinterpret_cast<char*>(b),4))return false;value=uint32_t(b[0])|(uint32_t(b[1])<<8)|(uint32_t(b[2])<<16)|(uint32_t(b[3])<<24);return true;}
bool Field(std::ifstream& file,std::string& value){uint32_t n;if(!Number(file,n)||n>65536)return false;value.resize(n);return !n||bool(file.read(value.data(),n));}
void Load(){
    const auto& id=Settings::Instance().LocaleId();if(id=="en"||id=="tr")return;
    std::ifstream file(Platform::ExeDir()/"languages"/id/"runtime.bin",std::ios::binary);
    char magic[8];uint32_t count;if(!file.read(magic,8)||std::memcmp(magic,"PMLOC1\0\0",8)||!Number(file,count)||count>10000)return;
    std::unordered_map<std::string,std::string> parsed;size_t bytes=0;
    for(uint32_t i=0;i<count;i++){std::string source,text;if(!Field(file,source)||!Field(file,text)||(bytes+=source.size()+text.size())>12*1024*1024||source.find('\0')!=std::string::npos||text.find('\0')!=std::string::npos)return;if(!text.empty())parsed[source]=text;}
    strings=std::move(parsed);std::cout<<"[PatriotsLocale] "<<id<<" runtime strings="<<strings.size()<<std::endl;
}
}
const char* Text(const char* tr,const char* en){if(Settings::Instance().LocaleId()=="tr")return tr;std::call_once(once,Load);auto i=strings.find(en);return i==strings.end()?en:i->second.c_str();}
}
