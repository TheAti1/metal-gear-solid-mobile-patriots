#include "touchcontrols.h"
#include "touch_stick.h"
#include "settings.h"
#include "movement.h"
#include <cassert>
#include <cmath>
#include <iostream>
#include <limits>
#include <vector>

static float sx=0,sy=0;
Settings::Settings(){ResetTouchLayout();}
Settings& Settings::Instance(){static Settings s;return s;}
void Settings::ResetTouchLayout(){for(int c=0;c<kTouchControls;c++){float x,y,z;TouchControls::DefaultPlace(c,x,y,z);touch_x[c]=x;touch_y[c]=y;touch_size[c]=z;}}
const std::vector<PhoneAction>& PhoneActions(){static const std::vector<PhoneAction> rows={{"up","",10},{"down","",12},{"left","",9},{"right","",11},{"a","",200},{"b","",201},{"ok","",78},{"lsk","",73},{"rsk","",72}};return rows;}
void Movement::SetStick(Source,float x,float y){sx=x;sy=y;}
int main(){
    auto& settings=Settings::Instance();settings.touch_mode=2;
    auto& controls=TouchControls::Instance();bool keys[256]={};int menus=0;
    controls.SetKeyCallback([&](const bool* next){for(int i=0;i<256;i++)keys[i]=next[i];});
    controls.SetMenuCallback([&](){menus++;});
    const int w=1280,h=800;float x=settings.touch_x[0]*w,y=settings.touch_y[0]*h;
    controls.SetMenuAvailable(false);controls.FingerDown(1,x,y,w,h);
    assert(sx==0&&sy==0&&!keys[78]); // resting thumb never confirms
    const float angle=.59f;
    controls.FingerMove(1,x+70*std::cos(angle),y+70*std::sin(angle),w,h);
    assert(keys[11]&&keys[12]&&!keys[78]);
    assert(std::fabs(std::atan2(sy,sx)-angle)<.0001f); // exact angle, not an 8-way snap
    controls.FingerDown(2,settings.touch_x[1]*w,settings.touch_y[1]*h,w,h);
    assert(keys[200]&&keys[11]); // two-finger move + action
    controls.FingerMove(1,x,y,w,h);assert(!keys[11]&&!keys[12]&&!keys[78]&&keys[200]);
    controls.FingerMove(1,x-100,y,w,h);assert(keys[9]&&sx<-.99f);
    controls.FingerDown(3,x+90,y,w,h); // a second thumb cannot steal the stick
    controls.FingerMove(3,x+130,y,w,h);assert(sx<-.99f);
    controls.FingerCancel(1);assert(sx==0&&sy==0&&keys[200]&&!keys[9]);
    controls.FingerUp(2,settings.touch_x[1]*w,settings.touch_y[1]*h,w,h);assert(!keys[200]);
    controls.FingerDown(4,settings.touch_x[1]*w-.22f*h,settings.touch_y[1]*h+.02f*h,w,h);
    assert(keys[78]);controls.FingerCancel(4);assert(!keys[78]);
    controls.SetMenuAvailable(true);controls.FingerDown(5,w*.5f,h*.05f,w,h);controls.FingerCancel(5);assert(menus==0);
    controls.FingerDown(6,w*.5f,h*.05f,w,h);controls.FingerUp(6,w*.5f,h*.05f,w,h);assert(menus==1);
    controls.FingerDown(7,x+60,y,w,h);controls.ReleaseAll();assert(sx==0&&sy==0);
    for(bool down:keys)assert(!down);
    auto state=TouchStick::Sample(.2f,0,false);assert(!state.held);
    state=TouchStick::Sample(.2f,0,true);assert(state.held);
    state=TouchStick::Sample(.1f,0,true);assert(!state.held);
    state=TouchStick::Sample(20,20,false);assert(std::fabs(std::hypot(state.x,state.y)-1)<.0001f);
    state=TouchStick::Sample(std::numeric_limits<float>::quiet_NaN(),0,true);assert(!state.held);
    std::cout<<"PASS: exact stick angle, dead zone, no centre confirm, multi-touch, ownership, release/cancel and bounds\n";
}
