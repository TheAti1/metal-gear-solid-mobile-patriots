// SPDX-License-Identifier: GPL-2.0-or-later
#pragma once
#include <cstdint>
#include <stdexcept>

namespace PatriotsInput {
inline uint32_t NameHash(const char* name) {
    uint32_t hash=5381;
    for(;*name;++name){uint8_t c=static_cast<uint8_t>(*name);if(c>='A'&&c<='Z')c+=32;hash=hash*33+c;}
    return hash;
}
// Applied to the guest keyboard snapshot, after every physical device has been
// mapped to phone actions. All three state bits (held/pressed/released) follow
// the same source key. Movement, pause, Codec and front-end menus stay unchanged.
inline uint32_t InventoryKey(uint32_t key,uint32_t state,bool invert) {
    if(!invert||state!=NameHash("CGameStateInventory"))return key;
    switch(key){case 0x0A:return 0x0C;case 0x0C:return 0x0A;
        case 0x0F:return 0x15;case 0x15:return 0x0F;default:return key;}
}
inline void SelfTest() {
    const auto inventory=NameHash("CGameStateInventory");
    for(uint32_t key=0;key<256;key++){
        if(InventoryKey(key,inventory,false)!=key)throw std::runtime_error("inventory inversion off");
        for(const char* state:{"CGameStateMain","CGameStatePause","CGameStateCodec","CGameStateFrontend"})
            if(InventoryKey(key,NameHash(state),true)!=key)throw std::runtime_error("inventory inversion leaked to another screen");
        if(InventoryKey(InventoryKey(key,inventory,true),inventory,true)!=key)throw std::runtime_error("inventory mapping is not reversible");
    }
    if(InventoryKey(0x0A,inventory,true)!=0x0C||InventoryKey(0x0C,inventory,true)!=0x0A||InventoryKey(0x0F,inventory,true)!=0x15)
        throw std::runtime_error("inventory up/down mapping");
}
}
