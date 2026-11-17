// SPDX-License-Identifier: GPL-2.0-or-later
#pragma once
#include "cpu.h"
#include <string>
namespace PatriotsPrompts {
void Init(Cpu* cpu);
void KeyboardInput(int vk);
void PadInput(int type);
void PadDisconnected();
std::string Rewrite(const std::string& text);
std::string Label(const std::string& action);
void SelfTest();
}
