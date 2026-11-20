#pragma once
#include <cmath>
// Screen-space virtual stick. The knob has continuous travel; direction keys
// retain menu navigation while Movement receives the exact unsnapped angle.
namespace TouchStick {
struct State { float x=0,y=0; bool held=false; unsigned directions=0; };
inline State Sample(float x,float y,bool wasHeld){
    State s;if(!std::isfinite(x)||!std::isfinite(y))return s;
    const float length=std::sqrt(x*x+y*y);
    if(!std::isfinite(length))return s;
    const float scale=length>1?1/length:1;s.x=x*scale;s.y=y*scale;
    s.held=length>(wasHeld?.16f:.22f);
    if(s.held){
        constexpr float tan22=.414213562f;
        if(std::fabs(x)>tan22*std::fabs(y))s.directions|=x>0?2u:8u;
        if(std::fabs(y)>tan22*std::fabs(x))s.directions|=y>0?4u:1u;
    }
    return s;
}
inline bool Contains(float cx,float cy,float radius,float x,float y){
    const float dx=x-cx,dy=y-cy;return dx*dx+dy*dy<=radius*radius;
}
}
