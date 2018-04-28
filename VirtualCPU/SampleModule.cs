using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using DotN64;
using DotN64.CPU;
using DotN64.Extensions;
using DotN64.Helpers;
using Nancy;
using Nancy.Responses;

namespace VirtualCPU
{
    public class SampleModule : NancyModule
    {
        private readonly TimeSpan executionTimeLimit = TimeSpan.FromSeconds(3.0);

        public SampleModule()
        {
            Get["/"] = _ => View["index.html"];
            Post["/"] = r =>
            {
                var data = ((string)Request.Form["data"]).Replace("\n", string.Empty).Replace("\r", string.Empty).Replace(" ", string.Empty);
                var instructionCount = Math.Min(data.Length / 8, 2048);
                var instructions = new List<VR4300.Instruction>(instructionCount);
                var ram = new byte[0x0100];

                for (int i = 0; i < instructionCount; i++)
                {
                    if (uint.TryParse(new string(data.Skip(i * 8).Take(8).ToArray()), NumberStyles.HexNumber, null, out var instruction))
                        instructions.Add(instruction);
                    else
                        return "BAD BAD BAD instruction";
                }

                var stop = false;
                var maps = new[]
                {
                    new MappingEntry(0x1FC00000, 0xC0000000)
                    {
                        Read = a =>
                        {
                            var index = (int)(a / 4);

                            if (index >= instructions.Count)
                                stop = true;

                            return index < instructions.Count ? instructions[index] : 0;
                        }
                    },
                    new MappingEntry(0x00000000, (uint)ram.Length)
                    {
                        Read = a => BitConverter.ToUInt32(ram, (int)a),
                        Write = (a, v) => BitHelper.Write(ram, (int)a, v)
                    }
                };
                var cpu = new VR4300
                {
                    ReadSysAD = maps.ReadWord,
                    WriteSysAD = maps.WriteWord
                };
                var timer = new Stopwatch();

                cpu.Reset();

                try
                {
                    timer.Start();

                    while (!stop && timer.Elapsed < executionTimeLimit)
                    {
                        cpu.Cycle();
                    }

                    timer.Stop();
                }
                catch (Exception e)
                {
                    return $"NOT OK !!! PLEASE UNDERSTAND {e.Message}";
                }

                var memory = new StringBuilder();

                memory.AppendLine("Memory be like:");
                memory.AppendLine();

                for (int i = 0; i < ram.Length; i += sizeof(uint))
                {
                    memory.AppendLine($"{i:X2}: 0x{BitConverter.ToUInt32(ram, i):X8}");
                }

                return new TextResponse("OK !!!\n" + memory + $"\nRuntime like {timer.Elapsed}");
            };
        }
    }
}
