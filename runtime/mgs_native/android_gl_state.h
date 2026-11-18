// SPDX-License-Identifier: GPL-2.0-or-later
#pragma once
// Android fixed-pipeline state used by the guest and the overlay. GLES1 state
// queries can be incomplete in translation drivers. Keep the state we change,
// including matrices, instead of restoring an overlay from unreliable queries.
// CPU matrix stacks also avoid the small ES projection/texture stack limits.
#include <array>
#include <vector>
#include <unordered_map>
#include <cstring>
#include <cmath>
namespace MgsGL {
using Matrix=std::array<GLfloat,16>;
inline Matrix Identity(){return {1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1};}
inline thread_local std::unordered_map<GLenum,GLboolean> caps{{GL_DITHER,GL_TRUE}};
inline thread_local GLenum active=GL_TEXTURE0,client=GL_TEXTURE0,mode=GL_MODELVIEW,blend_src=GL_ONE,blend_dst=GL_ZERO,shade=GL_SMOOTH;
inline thread_local GLboolean texture[2]={},depth=GL_TRUE,mask[4]={GL_TRUE,GL_TRUE,GL_TRUE,GL_TRUE};
inline thread_local GLfloat color[4]={1,1,1,1},clear[4]={};
inline thread_local GLint bound[2]={},env[2]={GL_MODULATE,GL_MODULATE},viewport[4]={},scissor[4]={},pack=4,unpack=4;
inline thread_local std::vector<Matrix> matrices[4]={{Identity()},{Identity()},{Identity()},{Identity()}};
struct Sampler{GLint min=GL_NEAREST_MIPMAP_LINEAR,mag=GL_LINEAR;};
inline thread_local std::unordered_map<GLuint,Sampler> samplers;
inline int Unit(){return active==GL_TEXTURE0+1?1:0;}
inline std::vector<Matrix>& Stack(){return matrices[mode==GL_PROJECTION?1:mode==GL_TEXTURE?2+Unit():0];}
inline void Enable(GLenum c){glEnable(c);if(c==GL_TEXTURE_2D)texture[Unit()]=GL_TRUE;else caps[c]=GL_TRUE;}
inline void Disable(GLenum c){glDisable(c);if(c==GL_TEXTURE_2D)texture[Unit()]=GL_FALSE;else caps[c]=GL_FALSE;}
inline GLboolean IsEnabled(GLenum c){if(c==GL_TEXTURE_2D)return texture[Unit()];auto i=caps.find(c);return i==caps.end()?GL_FALSE:i->second;}
inline void ActiveTexture(GLenum t){glActiveTexture(t);if(t>=GL_TEXTURE0&&t<GL_TEXTURE0+2)active=t;}
inline void ClientActiveTexture(GLenum t){glClientActiveTexture(t);if(t>=GL_TEXTURE0&&t<GL_TEXTURE0+2)client=t;}
inline void DepthMask(GLboolean d){glDepthMask(d);depth=d;}
inline void ColorMask(GLboolean r,GLboolean g,GLboolean b,GLboolean a){glColorMask(r,g,b,a);mask[0]=r;mask[1]=g;mask[2]=b;mask[3]=a;}
inline void Color4f(GLfloat r,GLfloat g,GLfloat b,GLfloat a){glColor4f(r,g,b,a);color[0]=r;color[1]=g;color[2]=b;color[3]=a;}
inline void ClearColor(GLfloat r,GLfloat g,GLfloat b,GLfloat a){glClearColor(r,g,b,a);clear[0]=r;clear[1]=g;clear[2]=b;clear[3]=a;}
inline void BindTexture(GLenum t,GLuint id){glBindTexture(t,id);if(t==GL_TEXTURE_2D)bound[Unit()]=id;}
inline void DeleteTextures(GLsizei n,const GLuint *ids){glDeleteTextures(n,ids);for(int i=0;i<n;i++){samplers.erase(ids[i]);for(int u=0;u<2;u++)if(bound[u]==(GLint)ids[i])bound[u]=0;}}
inline void TexParameteri(GLenum t,GLenum p,GLint v){glTexParameteri(t,p,v);if(t==GL_TEXTURE_2D){if(p==GL_TEXTURE_MIN_FILTER)samplers[bound[Unit()]].min=v;if(p==GL_TEXTURE_MAG_FILTER)samplers[bound[Unit()]].mag=v;}}
inline void GetTexParameteriv(GLenum t,GLenum p,GLint *v){if(t==GL_TEXTURE_2D&&(p==GL_TEXTURE_MIN_FILTER||p==GL_TEXTURE_MAG_FILTER)){auto &s=samplers[bound[Unit()]];*v=p==GL_TEXTURE_MIN_FILTER?s.min:s.mag;return;}glGetTexParameteriv(t,p,v);}
inline void TexEnvi(GLenum t,GLenum p,GLint v){glTexEnvi(t,p,v);if(t==GL_TEXTURE_ENV&&p==GL_TEXTURE_ENV_MODE)env[Unit()]=v;}
inline void TexEnvf(GLenum t,GLenum p,GLfloat v){glTexEnvf(t,p,v);if(t==GL_TEXTURE_ENV&&p==GL_TEXTURE_ENV_MODE)env[Unit()]=(GLint)v;}
inline void GetTexEnviv(GLenum t,GLenum p,GLint *v){if(t==GL_TEXTURE_ENV&&p==GL_TEXTURE_ENV_MODE){*v=env[Unit()];return;}glGetTexEnviv(t,p,v);}
inline void Viewport(GLint x,GLint y,GLsizei w,GLsizei h){glViewport(x,y,w,h);if(w>=0&&h>=0){viewport[0]=x;viewport[1]=y;viewport[2]=w;viewport[3]=h;}}
inline void Scissor(GLint x,GLint y,GLsizei w,GLsizei h){glScissor(x,y,w,h);if(w>=0&&h>=0){scissor[0]=x;scissor[1]=y;scissor[2]=w;scissor[3]=h;}}
inline void BlendFunc(GLenum s,GLenum d){glBlendFunc(s,d);blend_src=s;blend_dst=d;}
inline void ShadeModel(GLenum s){glShadeModel(s);shade=s;}
inline void PixelStorei(GLenum p,GLint v){glPixelStorei(p,v);if(p==GL_PACK_ALIGNMENT)pack=v;if(p==GL_UNPACK_ALIGNMENT)unpack=v;}
inline void MatrixMode(GLenum m){glMatrixMode(m);if(m==GL_MODELVIEW||m==GL_PROJECTION||m==GL_TEXTURE)mode=m;}
inline void LoadIdentity(){Stack().back()=Identity();glLoadIdentity();}
inline void LoadMatrixf(const GLfloat *m){std::memcpy(Stack().back().data(),m,sizeof(Matrix));glLoadMatrixf(m);}
inline void MultMatrixf(const GLfloat *m){Matrix a=Stack().back(),b{};for(int c=0;c<4;c++)for(int r=0;r<4;r++)for(int k=0;k<4;k++)b[r+c*4]+=a[r+k*4]*m[k+c*4];Stack().back()=b;glLoadMatrixf(b.data());}
inline void PushMatrix(){Stack().push_back(Stack().back());}
inline void PopMatrix(){auto &s=Stack();if(s.size()>1){s.pop_back();glLoadMatrixf(s.back().data());}}
inline void Translatef(GLfloat x,GLfloat y,GLfloat z){Matrix m=Identity();m[12]=x;m[13]=y;m[14]=z;MultMatrixf(m.data());}
inline void Scalef(GLfloat x,GLfloat y,GLfloat z){Matrix m=Identity();m[0]=x;m[5]=y;m[10]=z;MultMatrixf(m.data());}
inline void Rotatef(GLfloat angle,GLfloat x,GLfloat y,GLfloat z){float n=std::sqrt(x*x+y*y+z*z);if(n==0)return;x/=n;y/=n;z/=n;float a=angle*0.017453292519943295f,c=std::cos(a),s=std::sin(a),q=1-c;Matrix m={x*x*q+c,y*x*q+z*s,z*x*q-y*s,0,x*y*q-z*s,y*y*q+c,z*y*q+x*s,0,x*z*q+y*s,y*z*q-x*s,z*z*q+c,0,0,0,0,1};MultMatrixf(m.data());}
inline void Orthof(GLfloat l,GLfloat r,GLfloat b,GLfloat t,GLfloat n,GLfloat f){if(l==r||b==t||n==f)return;Matrix m=Identity();m[0]=2/(r-l);m[5]=2/(t-b);m[10]=-2/(f-n);m[12]=-(r+l)/(r-l);m[13]=-(t+b)/(t-b);m[14]=-(f+n)/(f-n);MultMatrixf(m.data());}
inline void Frustumf(GLfloat l,GLfloat r,GLfloat b,GLfloat t,GLfloat n,GLfloat f){if(l==r||b==t||n==f||n<=0||f<=0)return;Matrix m{};m[0]=2*n/(r-l);m[5]=2*n/(t-b);m[8]=(r+l)/(r-l);m[9]=(t+b)/(t-b);m[10]=-(f+n)/(f-n);m[11]=-1;m[14]=-2*f*n/(f-n);MultMatrixf(m.data());}
inline void GetBooleanv(GLenum c,GLboolean *v){if(c==GL_COLOR_WRITEMASK){std::memcpy(v,mask,4);return;}if(c==GL_DEPTH_WRITEMASK){*v=depth;return;}*v=IsEnabled(c);}
inline void GetFloatv(GLenum c,GLfloat *v){const GLfloat *p=nullptr;if(c==GL_CURRENT_COLOR)p=color;else if(c==GL_COLOR_CLEAR_VALUE)p=clear;else if(c==GL_MODELVIEW_MATRIX)p=matrices[0].back().data();else if(c==GL_PROJECTION_MATRIX)p=matrices[1].back().data();else if(c==GL_TEXTURE_MATRIX)p=matrices[2+Unit()].back().data();if(p){std::memcpy(v,p,(c==GL_CURRENT_COLOR||c==GL_COLOR_CLEAR_VALUE)?sizeof color:sizeof(Matrix));return;}glGetFloatv(c,v);}
inline void GetIntegerv(GLenum p,GLint *v){switch(p){case GL_ACTIVE_TEXTURE:*v=active;return;case GL_CLIENT_ACTIVE_TEXTURE:*v=client;return;case GL_TEXTURE_BINDING_2D:*v=bound[Unit()];return;case GL_VIEWPORT:std::memcpy(v,viewport,sizeof viewport);return;case GL_SCISSOR_BOX:std::memcpy(v,scissor,sizeof scissor);return;case GL_BLEND_SRC:*v=blend_src;return;case GL_BLEND_DST:*v=blend_dst;return;case GL_SHADE_MODEL:*v=shade;return;case GL_PACK_ALIGNMENT:*v=pack;return;case GL_UNPACK_ALIGNMENT:*v=unpack;return;case GL_MATRIX_MODE:*v=mode;return;case GL_MODELVIEW_STACK_DEPTH:*v=(GLint)matrices[0].size();return;case GL_PROJECTION_STACK_DEPTH:*v=(GLint)matrices[1].size();return;case GL_TEXTURE_STACK_DEPTH:*v=(GLint)matrices[2+Unit()].size();return;default:glGetIntegerv(p,v);}}
}
#define glEnable MgsGL::Enable
#define glDisable MgsGL::Disable
#define glIsEnabled MgsGL::IsEnabled
#define glActiveTexture MgsGL::ActiveTexture
#define glClientActiveTexture MgsGL::ClientActiveTexture
#define glDepthMask MgsGL::DepthMask
#define glColorMask MgsGL::ColorMask
#define glColor4f MgsGL::Color4f
#define glClearColor MgsGL::ClearColor
#define glBindTexture MgsGL::BindTexture
#define glDeleteTextures MgsGL::DeleteTextures
#define glTexParameteri MgsGL::TexParameteri
#define glGetTexParameteriv MgsGL::GetTexParameteriv
#define glTexEnvi MgsGL::TexEnvi
#define glTexEnvf MgsGL::TexEnvf
#define glGetTexEnviv MgsGL::GetTexEnviv
#define glViewport MgsGL::Viewport
#define glScissor MgsGL::Scissor
#define glBlendFunc MgsGL::BlendFunc
#define glShadeModel MgsGL::ShadeModel
#define glPixelStorei MgsGL::PixelStorei
#define glMatrixMode MgsGL::MatrixMode
#define glLoadIdentity MgsGL::LoadIdentity
#define glLoadMatrixf MgsGL::LoadMatrixf
#define glMultMatrixf MgsGL::MultMatrixf
#define glPushMatrix MgsGL::PushMatrix
#define glPopMatrix MgsGL::PopMatrix
#define glTranslatef MgsGL::Translatef
#define glScalef MgsGL::Scalef
#define glRotatef MgsGL::Rotatef
#define glOrthof MgsGL::Orthof
#define glFrustumf MgsGL::Frustumf
#define glGetBooleanv MgsGL::GetBooleanv
#define glGetFloatv MgsGL::GetFloatv
#define glGetIntegerv MgsGL::GetIntegerv
