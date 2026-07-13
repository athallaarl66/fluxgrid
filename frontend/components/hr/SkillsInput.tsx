"use client";

import { useState } from "react";
import { X } from "lucide-react";

export function SkillsInput({
  skills,
  onChange,
}: {
  skills: string[];
  onChange: (skills: string[]) => void;
}) {
  const [input, setInput] = useState("");

  function add() {
    const trimmed = input.trim();
    if (!trimmed || skills.includes(trimmed)) return;
    onChange([...skills, trimmed]);
    setInput("");
  }

  function remove(skill: string) {
    onChange(skills.filter((s) => s !== skill));
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap gap-1.5">
        {skills.map((skill) => (
          <span
            key={skill}
            className="inline-flex items-center gap-1 rounded-full border border-[#C5D89D] bg-[#F6F0D7] px-2.5 py-0.5 text-[11px] text-foreground"
          >
            {skill}
            <button
              type="button"
              onClick={() => remove(skill)}
              className="size-3 rounded-full hover:bg-black/10 inline-flex items-center justify-center cursor-pointer"
            >
              <X className="size-2.5" />
            </button>
          </span>
        ))}
      </div>
      <div className="flex gap-1">
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); add(); } }}
          placeholder="Type skill and press Enter..."
          className="flex-1 h-7 rounded-md border border-input bg-transparent px-2 text-xs outline-none focus-visible:ring-1 focus-visible:ring-ring"
        />
      </div>
    </div>
  );
}
