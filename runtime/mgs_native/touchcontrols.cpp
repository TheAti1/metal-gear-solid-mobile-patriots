#include "touchcontrols.h"
#include "movement.h"
#include "settings.h"
#include "touch_shapes.h"
#include "touch_stick.h"

#include "imgui.h"
#include <algorithm>
#include <cmath>
#include <cstring>
#include <vector>

namespace {

const TouchShapes::Shape* const kShapes[TouchControls::COUNT] = { &TouchShapes::dpad, &TouchShapes::a, &TouchShapes::lsk, &TouchShapes::rsk, &TouchShapes::b };
// All controls in one colour (#d1d4f8; the artwork's colours only tell them apart), at the opacity setting.
const ImU32 kColour = IM_COL32(0xD1, 0xD4, 0xF8, 255);

uint8_t PhoneKey(const char* id) {
    for (const PhoneAction& a : PhoneActions()) if (strcmp(a.id, id) == 0) return a.key;
    return 0;
}

ImU32 WithAlpha(ImU32 c, float a) {
    const int alpha = static_cast<int>(((c >> IM_COL32_A_SHIFT) & 0xFF) * std::clamp(a, 0.0f, 1.0f));
    return (c & ~IM_COL32_A_MASK) | (static_cast<ImU32>(alpha) << IM_COL32_A_SHIFT);
}

} // namespace

TouchControls& TouchControls::Instance() {
    static TouchControls t;
    return t;
}

const char* TouchControls::Name(int c) { static const char* n[] = { "dpad", "a", "lsk", "rsk", "b" }; return n[c]; }
const char* TouchControls::Label(int c) {
    static const char* n[] = { "D-pad (centre = OK)", "A button", "Left soft key (pause menu)", "Right soft key (equipment)",
                              "B button (first-person view)" };
    return n[c];
}

// Default layout: the D-pad bottom left; the A button bottom right with the soft keys and B around it as in
// the artwork (scaled with A, for a 4:3 frame). Centre x / y as fractions of the frame, size as a fraction of
// its height.
void TouchControls::DefaultPlace(int c, float& x, float& y, float& size) {
#if defined(__ANDROID__) || defined(PATRIOTS_TOUCH_ANDROID_TEST)
    static const float places[COUNT][3]={{.15f,.76f,.34f},{.89f,.78f,.17f},{.94f,.10f,.09f},{.77f,.55f,.115f},{.91f,.53f,.115f}};
    x=places[c][0];y=places[c][1];size=places[c][2];return;
#endif
    if (c == DPAD) { x = 0.17f; y = 0.72f; size = 0.40f; return; }
    const TouchShapes::Shape& a = TouchShapes::a;
    const TouchShapes::Shape& sh = *kShapes[c];
    const float ax = 0.83f, ay = 0.78f, asize = 0.20f, k = asize / a.h, aspect = 4.0f / 3.0f;
    x = ax + ((sh.x + sh.w / 2) - (a.x + a.w / 2)) * k / aspect;
    y = ay + ((sh.y + sh.h / 2) - (a.y + a.h / 2)) * k;
    size = sh.h * k;
}

uint32_t TouchControls::ButtonBit(int c) {
    static const uint32_t bits[COUNT] = { 0, B_A, B_LSK, B_RSK, B_B };
    return bits[c];
}

TouchControls::Rect TouchControls::Place(int c, int fw, int fh, float* scale) {
    Settings& s = Settings::Instance();
    const TouchShapes::Shape& sh = *kShapes[c];
    const float k = s.touch_size[c].load() * fh / sh.h;
    const float cx = s.touch_x[c].load() * fw, cy = s.touch_y[c].load() * fh;
    if (scale) *scale = k;
#if defined(__ANDROID__) || defined(PATRIOTS_TOUCH_ANDROID_TEST)
    const float radius=s.touch_size[c].load()*fh*.5f;
    return {cx-radius,cy-radius,cx+radius,cy+radius};
#endif
    return { cx - sh.w * k / 2, cy - sh.h * k / 2, cx + sh.w * k / 2, cy + sh.h * k / 2 };
}

TouchControls::Rect TouchControls::MenuRect(int fw, int fh) {
    const float w = 0.12f * fh, h = 0.075f * fh, top = 0.015f * fh;
    return { fw / 2.0f - w / 2, top, fw / 2.0f + w / 2, top + h };
}

TouchControls::Rect TouchControls::ConfirmRect(int fw,int fh){
    const Rect a=Place(A,fw,fh);const float r=.060f*fh;
    const float x=(a.x0+a.x1)*.5f-.22f*fh,y=(a.y0+a.y1)*.5f+.02f*fh;
    return {x-r,y-r,x+r,y+r};
}

bool TouchControls::Enabled() const { return Settings::Instance().touch_mode != 0; }
bool TouchControls::Visible() const {
    const int mode = Settings::Instance().touch_mode;
    return mode == 2 || (mode == 1 && shown_);
}

void TouchControls::OtherInput() {
    if (Settings::Instance().touch_mode == 1 && shown_.exchange(false)) ReleaseAll();
}

// D-pad: the centre button, else up to two directions (8-way, 45 degree sectors) around a small dead zone.
uint32_t TouchControls::DpadBits(float x, float y, int fw, int fh, float* vx, float* vy,bool* held) const {
    if (vx) *vx = *vy = 0;
    float k = 1;
    const Rect r = Place(DPAD, fw, fh, &k);
#if defined(__ANDROID__) || defined(PATRIOTS_TOUCH_ANDROID_TEST)
    const float radius=(r.x1-r.x0)*.5f*.65f;
    const auto sample=TouchStick::Sample((x-(r.x0+r.x1)*.5f)/radius,(y-(r.y0+r.y1)*.5f)/radius,held&&*held);
    if(vx){*vx=sample.x;*vy=sample.y;}if(held)*held=sample.held;return sample.directions;
#endif
    const TouchShapes::Shape& sh = TouchShapes::dpad;
    for (int i = 0; i < sh.count; ++i) {
        const TouchShapes::Path& p = sh.paths[i];
        if (p.role != 1) continue;
        float x0 = 1e9f, y0 = 1e9f, x1 = -1e9f, y1 = -1e9f;
        for (int j = 0; j < p.count; ++j) {
            x0 = std::min(x0, p.xy[2 * j]); x1 = std::max(x1, p.xy[2 * j]);
            y0 = std::min(y0, p.xy[2 * j + 1]); y1 = std::max(y1, p.xy[2 * j + 1]);
        }
        if (x >= r.x0 + x0 * k && x <= r.x0 + x1 * k && y >= r.y0 + y0 * k && y <= r.y0 + y1 * k) return B_OK;
    }
    const float dx = (x - (r.x0 + r.x1) / 2) / ((r.x1 - r.x0) / 2), dy = (y - (r.y0 + r.y1) / 2) / ((r.y1 - r.y0) / 2);
    if (dx * dx + dy * dy < 0.12f * 0.12f) return 0;
    if (vx) { const float m = std::sqrt(dx * dx + dy * dy); *vx = dx / m; *vy = dy / m; }   // (analog movement)
    uint32_t bits = 0;
    constexpr float kTan22 = 0.4142f;
    if (std::fabs(dx) > kTan22 * std::fabs(dy)) bits |= dx > 0 ? B_RIGHT : B_LEFT;
    if (std::fabs(dy) > kTan22 * std::fabs(dx)) bits |= dy > 0 ? B_DOWN : B_UP;
    return bits;
}

void TouchControls::FingerDown(int64_t id, float x, float y, int fw, int fh) {
    if (!Enabled() || fw <= 0 || fh <= 0) return;
    std::lock_guard<std::recursive_mutex> lock(mutex_);
    if (!Visible()) shown_ = true;   // Auto: the first touch brings them up (and counts)
    Finger f{-2, 0};
    if (menu_available_ && MenuRect(fw, fh).Contains(x, y, 0.02f * fh)) f = {-1, B_MENU};
#if defined(__ANDROID__) || defined(PATRIOTS_TOUCH_ANDROID_TEST)
    else if(ConfirmRect(fw,fh).Contains(x,y,.01f*fh))f={-3,B_OK};
#endif
    else {
        for (int c : {A, LSK, RSK, B}) {
            const Rect r = Place(c, fw, fh);
#if defined(__ANDROID__) || defined(PATRIOTS_TOUCH_ANDROID_TEST)
            const bool hit=TouchStick::Contains((r.x0+r.x1)*.5f,(r.y0+r.y1)*.5f,(r.y1-r.y0)*.63f,x,y);
#else
            const bool hit=r.Contains(x,y,.08f*(r.y1-r.y0));
#endif
            if (hit) { f = {c, ButtonBit(c)}; break; }
        }
        if (f.control == -2) {
            const Rect r = Place(DPAD, fw, fh);
            bool free=true;
#if defined(__ANDROID__) || defined(PATRIOTS_TOUCH_ANDROID_TEST)
            for(const auto& pair:fingers_)if(pair.second.control==DPAD&&pair.first!=id)free=false;
            const bool hit=TouchStick::Contains((r.x0+r.x1)*.5f,(r.y0+r.y1)*.5f,(r.y1-r.y0)*.60f,x,y);
#else
            const bool hit=r.Contains(x,y,.1f*(r.y1-r.y0));
#endif
            if(free&&hit){f={DPAD,0};f.bits=DpadBits(x,y,fw,fh,&f.vx,&f.vy,&f.held);}
        }
    }
    fingers_[id] = f;
    Update();
}

void TouchControls::FingerMove(int64_t id, float x, float y, int fw, int fh) {
    std::lock_guard<std::recursive_mutex> lock(mutex_);
    auto it = fingers_.find(id);
    if (it == fingers_.end() || fw <= 0 || fh <= 0) return;
    Finger& f = it->second;
    if (f.control == DPAD) f.bits = DpadBits(x, y, fw, fh, &f.vx, &f.vy,&f.held);   // slides between directions, even past the edge
    else if(f.control==-3)f.bits=ConfirmRect(fw,fh).Contains(x,y,.025f*fh)?B_OK:0;
    else if (f.control == -1) f.bits = MenuRect(fw, fh).Contains(x, y, 0.04f * fh) ? B_MENU : 0;
    else if (f.control >= 0) {
        const Rect r = Place(f.control, fw, fh);
        f.bits = r.Contains(x, y, 0.15f * (r.y1 - r.y0)) ? ButtonBit(f.control) : 0;   // sliding off lets go
    }
    Update();
}

void TouchControls::FingerUp(int64_t id, float x, float y, int fw, int fh) {
    bool menu_tap = false;
    {
        std::lock_guard<std::recursive_mutex> lock(mutex_);
        auto it = fingers_.find(id);
        if (it == fingers_.end()) return;
        FingerMove(id, x, y, fw, fh);
        menu_tap = it->second.control == -1 && (it->second.bits & B_MENU);
        fingers_.erase(it);
        Update();
    }
    if (menu_tap && menu_cb_) menu_cb_();   // on release, so the tap doesn't reach the menu it opens
}

void TouchControls::FingerCancel(int64_t id){
    std::lock_guard<std::recursive_mutex> lock(mutex_);fingers_.erase(id);Update();
}

void TouchControls::ReleaseAll() {
    std::lock_guard<std::recursive_mutex> lock(mutex_);
    fingers_.clear();
    Update();
}

void TouchControls::Update() {   // mutex_ held
    uint32_t bits = 0;
    float vx = 0, vy = 0;
    for (const auto& kv : fingers_) {
        bits |= kv.second.bits;
        if (kv.second.control == DPAD && (kv.second.bits & (B_UP | B_RIGHT | B_DOWN | B_LEFT))) { vx = kv.second.vx; vy = kv.second.vy; }
    }
    stick_x_=0;stick_y_=0;
    for(const auto& kv:fingers_)if(kv.second.control==DPAD){stick_x_=kv.second.vx;stick_y_=kv.second.vy;}
#if defined(__ANDROID__) || defined(PATRIOTS_TOUCH_ANDROID_TEST)
    // Recovered game speed comes from the animation; preserve it while passing
    // the exact unsnapped direction. The keys decide when movement starts/stops.
    const float length=std::sqrt(vx*vx+vy*vy);if(length>0){vx/=length;vy/=length;}
#endif
    Movement::SetStick(Movement::TOUCH, vx, vy);
    if (bits == pressed_.exchange(bits) || !keys_cb_) return;
    static const struct { uint32_t bit; uint8_t key; } map[] = {
        {B_UP, PhoneKey("up")}, {B_RIGHT, PhoneKey("right")}, {B_DOWN, PhoneKey("down")}, {B_LEFT, PhoneKey("left")},
        {B_OK, PhoneKey("ok")}, {B_A, PhoneKey("a")}, {B_B, PhoneKey("b")}, {B_LSK, PhoneKey("lsk")}, {B_RSK, PhoneKey("rsk")} };
    bool keys[256] = {};
    for (const auto& m : map) if (bits & m.bit) keys[m.key] = true;
#if defined(__ANDROID__) || defined(PATRIOTS_TOUCH_ANDROID_TEST)
    // The on-screen action button also confirms menus/cutscenes. Gameplay keeps
    // the original A action, while the D-pad centre remains a dedicated OK key.
    if(menu_available_ && (bits & B_A))keys[PhoneKey("ok")]=true;
#endif
    keys_cb_(keys);
}

void TouchControls::Draw(ImDrawList* dl, int fw, int fh, bool preview) {
    if (!dl || fw <= 0 || fh <= 0 || (!preview && !Visible())) return;
    const float alpha = Settings::Instance().touch_opacity / 100.0f;
    const uint32_t pressed = pressed_;
#if defined(__ANDROID__) || defined(PATRIOTS_TOUCH_ANDROID_TEST)
    const ImU32 idle=IM_COL32(226,235,224,255),green=IM_COL32(200,230,119,255);
    const float stroke=std::max(1.4f,fh*.0028f);
    auto circle=[&](float cx,float cy,float r,bool down){
        dl->AddCircleFilled(ImVec2(cx,cy),r,WithAlpha(IM_COL32(11,20,17,255),(down?.66f:.27f)*alpha),48);
        if(down)dl->AddCircleFilled(ImVec2(cx,cy),r,WithAlpha(green,.25f*alpha),48);
        dl->AddCircle(ImVec2(cx,cy),r,WithAlpha(down?green:idle,alpha),48,stroke);
    };
    const Rect pad=Place(DPAD,fw,fh);const float cx=(pad.x0+pad.x1)*.5f,cy=(pad.y0+pad.y1)*.5f,r=(pad.x1-pad.x0)*.5f;
    const float blend=1-std::exp(-24*std::min(.1f,ImGui::GetIO().DeltaTime));
    drawn_x_+=(stick_x_.load()-drawn_x_)*blend;drawn_y_+=(stick_y_.load()-drawn_y_)*blend;
    const bool moving=(pressed&15)!=0;circle(cx,cy,r,moving);
    dl->AddCircle(ImVec2(cx,cy),r*.66f,WithAlpha(idle,.22f*alpha),48,stroke*.7f);
    for(int i=0;i<4;i++){const float a=i*1.570796327f;dl->AddLine(ImVec2(cx+std::cos(a)*r*.86f,cy+std::sin(a)*r*.86f),ImVec2(cx+std::cos(a)*r*.94f,cy+std::sin(a)*r*.94f),WithAlpha(idle,.6f*alpha),stroke);}
    const ImVec2 knob(cx+drawn_x_*r*.65f,cy+drawn_y_*r*.65f);circle(knob.x,knob.y,r*.35f,moving);
    dl->AddCircleFilled(knob,r*.10f,WithAlpha(moving?green:idle,.8f*alpha),24);
    auto icon=[&](int c,const Rect& box,bool down){
        float x=(box.x0+box.x1)*.5f,y=(box.y0+box.y1)*.5f,r=(box.y1-box.y0)*.5f;
        circle(x,y,r,down);ImU32 col=WithAlpha(down?green:idle,alpha);float z=r*.42f;
        if(c==A){dl->AddCircle(ImVec2(x,y),z*.62f,col,24,stroke);dl->AddLine(ImVec2(x-z,y),ImVec2(x+z,y),col,stroke);dl->AddLine(ImVec2(x,y-z),ImVec2(x,y+z),col,stroke);}
        else if(c==LSK){dl->AddLine(ImVec2(x-z*.5f,y-z),ImVec2(x-z*.5f,y+z),col,stroke*1.8f);dl->AddLine(ImVec2(x+z*.5f,y-z),ImVec2(x+z*.5f,y+z),col,stroke*1.8f);}
        else if(c==B){dl->AddEllipse(ImVec2(x,y),ImVec2(z*1.2f,z*.7f),col,0,24,stroke);dl->AddCircleFilled(ImVec2(x,y),z*.28f,col,20);}
        else if(c==RSK){dl->AddRect(ImVec2(x-z,y-z*.65f),ImVec2(x+z,y+z),col,z*.15f,0,stroke);dl->AddRect(ImVec2(x-z*.45f,y-z*1.1f),ImVec2(x+z*.45f,y-z*.65f),col,z*.1f,0,stroke);}
        else{dl->AddLine(ImVec2(x-z,y),ImVec2(x-z*.15f,y+z*.7f),col,stroke*1.5f);dl->AddLine(ImVec2(x-z*.15f,y+z*.7f),ImVec2(x+z,y-z*.7f),col,stroke*1.5f);}
    };
    for(int c:{A,LSK,RSK,B})icon(c,Place(c,fw,fh),(pressed&ButtonBit(c))!=0);
    icon(-3,ConfirmRect(fw,fh),(pressed&B_OK)!=0);
    if(menu_available_||preview){const Rect box=MenuRect(fw,fh);const float x=(box.x0+box.x1)*.5f,y=(box.y0+box.y1)*.5f;const ImU32 col=WithAlpha(idle,alpha);dl->AddRectFilled(ImVec2(box.x0,box.y0),ImVec2(box.x1,box.y1),WithAlpha(IM_COL32(11,20,17,255),.4f*alpha),fh*.02f);for(int i=-1;i<=1;i++)dl->AddLine(ImVec2(x-fh*.022f,y+i*fh*.013f),ImVec2(x+fh*.022f,y+i*fh*.013f),col,stroke);}
    return;
#endif
    std::vector<ImVec2> pts;
    auto build = [&](const TouchShapes::Path& p, const Rect& r, float k) {
        pts.resize(p.count);
        for (int j = 0; j < p.count; ++j) pts[j] = ImVec2(r.x0 + p.xy[2 * j] * k, r.y0 + p.xy[2 * j + 1] * k);
    };
    for (int c = 0; c < COUNT; ++c) {
        float k = 1;
        const Rect r = Place(c, fw, fh, &k);
        const TouchShapes::Shape& sh = *kShapes[c];
        const bool button_down = (pressed & ButtonBit(c)) != 0;
        int outer = 0;   // the largest closed outline (filled while the button is held)
        float outer_size = -1;
        for (int i = 0; i < sh.count; ++i) {
            const TouchShapes::Path& p = sh.paths[i];
            float x0 = 1e9f, x1 = -1e9f;
            for (int j = 0; j < p.count; ++j) { x0 = std::min(x0, p.xy[2 * j]); x1 = std::max(x1, p.xy[2 * j]); }
            if (p.closed && x1 - x0 > outer_size) { outer_size = x1 - x0; outer = i; }
        }
        for (int i = 0; i < sh.count; ++i) {
            const TouchShapes::Path& p = sh.paths[i];
            build(p, r, k);
            const bool lit = (p.role == 1 && (pressed & B_OK)) || (p.role >= 2 && (pressed & (1u << (p.role - 2)))) ||
                             (button_down && i == outer);
            if (lit && p.closed) dl->AddConcavePolyFilled(pts.data(), p.count, WithAlpha(kColour, 0.45f * alpha));
            const float t = std::max(1.5f, p.width * k) * (lit && !p.closed ? 1.8f : 1.0f);
            const ImDrawFlags fl = p.closed ? ImDrawFlags_Closed : ImDrawFlags_None;
            dl->AddPolyline(pts.data(), p.count, IM_COL32(0, 0, 0, static_cast<int>(110 * alpha)), fl, t * 2.2f);   // contrast
            dl->AddPolyline(pts.data(), p.count, WithAlpha(lit ? IM_COL32(255, 255, 255, 255) : kColour, alpha), fl, t);
        }
    }
    if (menu_available_ || preview) {   // the menu button: a rounded frame with three bars
        const Rect m = MenuRect(fw, fh);
        const float t = std::max(1.5f, 0.0055f * fh), round = 0.25f * (m.y1 - m.y0);
        const ImU32 col = WithAlpha(kColour, alpha);
        if (pressed & B_MENU) dl->AddRectFilled(ImVec2(m.x0, m.y0), ImVec2(m.x1, m.y1), WithAlpha(kColour, 0.4f * alpha), round);
        dl->AddRect(ImVec2(m.x0, m.y0), ImVec2(m.x1, m.y1), IM_COL32(0, 0, 0, static_cast<int>(110 * alpha)), round, 0, t * 2.2f);
        dl->AddRect(ImVec2(m.x0, m.y0), ImVec2(m.x1, m.y1), col, round, 0, t);
        const float cx = (m.x0 + m.x1) / 2, cy = (m.y0 + m.y1) / 2, half = 0.22f * (m.x1 - m.x0), gap = 0.2f * (m.y1 - m.y0);
        for (int i = -1; i <= 1; ++i) dl->AddLine(ImVec2(cx - half, cy + i * gap), ImVec2(cx + half, cy + i * gap), col, t);
    }
}
