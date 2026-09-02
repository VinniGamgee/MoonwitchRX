from pathlib import Path

patcher_path = Path("src/Ryujinx.Cpu/Nce/NcePatcher.cs")
text = patcher_path.read_text()

old_load = '''        private static void WriteLoadContext(Assembler asm, Operand tmp0, Operand tmp1, Operand tmp2)
        {
            asm.Mov(tmp0, (ulong)NceThreadTable.EntriesPointer);

            if (OperatingSystem.IsMacOS())
            {
                asm.MrsTpidrroEl0(tmp1);
            }
            else
            {
                asm.MrsTpidrEl0(tmp1);
            }

            Operand lblFound = asm.CreateLabel();
            Operand lblLoop = asm.CreateLabel();

            asm.MarkLabel(lblLoop);

            asm.LdrRiPost(tmp2, tmp0, 16);
            asm.Cmp(tmp1, tmp2);
            asm.B(lblFound, ArmCondition.Eq);
            asm.B(lblLoop);

            asm.MarkLabel(lblFound);

            asm.Ldur(tmp0, tmp0, -8);
        }
'''

new_load = '''        private static void WriteLoadContext(Assembler asm, Operand tmp0, Operand tmp1, Operand tmp2)
        {
            if (OperatingSystem.IsMacOS())
            {
                asm.MrsTpidrroEl0(tmp1);
            }
            else
            {
                asm.MrsTpidrEl0(tmp1);
            }

            // RX9: Try the direct-mapped NCE context cache first. The cache key is
            // derived entirely in generated ARM64 code, avoiding a managed call and
            // avoiding the old O(N) scan on the overwhelmingly common hit path.
            asm.Mov(tmp0, (ulong)NceThreadTable.FastEntriesPointer);
            asm.Mov(tmp2, tmp1);
            asm.Eor(tmp2, tmp2, tmp2, ArmShiftType.Lsr, 17);
            asm.Lsr(tmp2, tmp2, Const(4));
            asm.And(tmp2, tmp2, Const((ulong)NceThreadTable.FastTableMask));
            asm.Lsl(tmp2, tmp2, Const(4));
            asm.Add(tmp0, tmp0, tmp2);
            asm.LdrRiUn(tmp2, tmp0, 0);

            Operand lblFallback = asm.CreateLabel();
            Operand lblDone = asm.CreateLabel();

            asm.Cmp(tmp1, tmp2);
            asm.B(lblFallback, ArmCondition.Ne);
            asm.LdrRiUn(tmp0, tmp0, 8);
            asm.B(lblDone);

            // Collision/miss: preserve the original behavior exactly.
            asm.MarkLabel(lblFallback);
            asm.Mov(tmp0, (ulong)NceThreadTable.EntriesPointer);

            Operand lblFound = asm.CreateLabel();
            Operand lblLoop = asm.CreateLabel();

            asm.MarkLabel(lblLoop);

            asm.LdrRiPost(tmp2, tmp0, 16);
            asm.Cmp(tmp1, tmp2);
            asm.B(lblFound, ArmCondition.Eq);
            asm.B(lblLoop);

            asm.MarkLabel(lblFound);
            asm.Ldur(tmp0, tmp0, -8);

            asm.MarkLabel(lblDone);
        }
'''

old_safe = '''        private static void WriteLoadContextSafe(Assembler asm, Operand lblFail, Operand tmp0, Operand tmp1, Operand tmp2, Operand tmp3)
        {
            asm.Mov(tmp0, (ulong)NceThreadTable.EntriesPointer);
            asm.Ldur(tmp3, tmp0, -8);
            asm.Add(tmp3, tmp0, tmp3, ArmShiftType.Lsl, 4);

            if (OperatingSystem.IsMacOS())
            {
                asm.MrsTpidrroEl0(tmp1);
            }
            else
            {
                asm.MrsTpidrEl0(tmp1);
            }

            Operand lblFound = asm.CreateLabel();
            Operand lblLoop = asm.CreateLabel();

            asm.MarkLabel(lblLoop);

            asm.Cmp(tmp0, tmp3);
            asm.B(lblFail, ArmCondition.GeUn);
            asm.LdrRiPost(tmp2, tmp0, 16);
            asm.Cmp(tmp1, tmp2);
            asm.B(lblFound, ArmCondition.Eq);
            asm.B(lblLoop);

            asm.MarkLabel(lblFound);

            asm.Ldur(tmp0, tmp0, -8);
        }
'''

new_safe = '''        private static void WriteLoadContextSafe(Assembler asm, Operand lblFail, Operand tmp0, Operand tmp1, Operand tmp2, Operand tmp3)
        {
            if (OperatingSystem.IsMacOS())
            {
                asm.MrsTpidrroEl0(tmp1);
            }
            else
            {
                asm.MrsTpidrEl0(tmp1);
            }

            // The signal/suspend-safe lookup gets the same O(1) hot cache, but on
            // any mismatch it retains the original bounded scan and failure path.
            asm.Mov(tmp0, (ulong)NceThreadTable.FastEntriesPointer);
            asm.Mov(tmp2, tmp1);
            asm.Eor(tmp2, tmp2, tmp2, ArmShiftType.Lsr, 17);
            asm.Lsr(tmp2, tmp2, Const(4));
            asm.And(tmp2, tmp2, Const((ulong)NceThreadTable.FastTableMask));
            asm.Lsl(tmp2, tmp2, Const(4));
            asm.Add(tmp0, tmp0, tmp2);
            asm.LdrRiUn(tmp2, tmp0, 0);

            Operand lblFallback = asm.CreateLabel();
            Operand lblDone = asm.CreateLabel();

            asm.Cmp(tmp1, tmp2);
            asm.B(lblFallback, ArmCondition.Ne);
            asm.LdrRiUn(tmp0, tmp0, 8);
            asm.B(lblDone);

            asm.MarkLabel(lblFallback);
            asm.Mov(tmp0, (ulong)NceThreadTable.EntriesPointer);
            asm.Ldur(tmp3, tmp0, -8);
            asm.Add(tmp3, tmp0, tmp3, ArmShiftType.Lsl, 4);

            Operand lblFound = asm.CreateLabel();
            Operand lblLoop = asm.CreateLabel();

            asm.MarkLabel(lblLoop);

            asm.Cmp(tmp0, tmp3);
            asm.B(lblFail, ArmCondition.GeUn);
            asm.LdrRiPost(tmp2, tmp0, 16);
            asm.Cmp(tmp1, tmp2);
            asm.B(lblFound, ArmCondition.Eq);
            asm.B(lblLoop);

            asm.MarkLabel(lblFound);
            asm.Ldur(tmp0, tmp0, -8);

            asm.MarkLabel(lblDone);
        }
'''

if old_load not in text:
    raise SystemExit("RX9 patch failed: WriteLoadContext baseline not found")
if old_safe not in text:
    raise SystemExit("RX9 patch failed: WriteLoadContextSafe baseline not found")

text = text.replace(old_load, new_load, 1).replace(old_safe, new_safe, 1)
patcher_path.write_text(text)

build_path = Path("src/KenjinxAndroid/app/build.gradle")
build = build_path.read_text()
old_version = "versionName '2.1.0-pr.2-rx8-presentqueue'"
new_version = "versionName '2.1.0-pr.2-rx9-nce-fastlookup'"
if old_version not in build:
    raise SystemExit("RX9 patch failed: RX8 versionName baseline not found")
build_path.write_text(build.replace(old_version, new_version, 1))

print("RX9 NCE fast lookup patch applied")
