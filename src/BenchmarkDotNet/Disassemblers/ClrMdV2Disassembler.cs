﻿using BenchmarkDotNet.Filters;
using Iced.Intel;
using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BenchmarkDotNet.Disassemblers
{
    // This Disassembler uses ClrMd v2x. Please keep it in sync with ClrMdV1Disassembler (if possible).
    internal static class ClrMdV2Disassembler
    {
        internal static DisassemblyResult AttachAndDisassemble(Settings settings)
        {
            using (var dataTarget = DataTarget.AttachToProcess(
                settings.ProcessId,
                suspend: false))
            {
                var runtime = dataTarget.ClrVersions.Single().CreateRuntime();

                ConfigureSymbols(dataTarget);

                var state = new State(runtime);

                if (settings.Filters.Length > 0)
                {
                    FilterAndEnqueue(state, settings);
                }
                else
                {
                    ClrType typeWithBenchmark = state.Runtime.EnumerateModules().Select(module => module.GetTypeByName(settings.TypeName)).First(type => type != null);

                    state.Todo.Enqueue(
                        new MethodInfo(
                            // the Disassembler Entry Method is always parameterless, so check by name is enough
                            typeWithBenchmark.Methods.Single(method => method.IsPublic && method.Name == settings.MethodName),
                            0));
                }

                var disassembledMethods = Disassemble(settings, state);

                // we don't want to export the disassembler entry point method which is just an artificial method added to get generic types working
                var filteredMethods = disassembledMethods.Length == 1
                    ? disassembledMethods // if there is only one method we want to return it (most probably benchmark got inlined)
                    : disassembledMethods.Where(method => !method.Name.Contains(DisassemblerConstants.DisassemblerEntryMethodName)).ToArray();

                return new DisassemblyResult
                {
                    Methods = filteredMethods,
                    SerializedAddressToNameMapping = state.AddressToNameMapping.Select(x => new DisassemblyResult.MutablePair { Key = x.Key, Value = x.Value }).ToArray(),
                    PointerSize = (uint)IntPtr.Size
                };
            }
        }

        private static void ConfigureSymbols(DataTarget dataTarget)
        {
            // code copied from https://github.com/Microsoft/clrmd/issues/34#issuecomment-161926535
            dataTarget.SetSymbolPath("http://msdl.microsoft.com/download/symbols");
        }

        private static void FilterAndEnqueue(State state, Settings settings)
        {
            Regex[] filters = GlobFilter.ToRegex(settings.Filters);

            foreach (ClrModule module in state.Runtime.EnumerateModules())
                foreach (ClrType type in module.EnumerateTypeDefToMethodTableMap().Select(map => state.Runtime.GetTypeByMethodTable(map.MethodTable)).Where(type => type is not null))
                    foreach (ClrMethod method in type.Methods.Where(method => CanBeDisassembled(method) && method.Signature != null))
                        foreach (Regex filter in filters)
                        {
                            if (filter.IsMatch(method.Signature))
                            {
                                state.Todo.Enqueue(new MethodInfo(method,
                                    depth: settings.MaxDepth)); // don't allow for recursive disassembling
                                break;
                            }
                        }
        }

        private static DisassembledMethod[] Disassemble(Settings settings, State state)
        {
            var result = new List<DisassembledMethod>();

            while (state.Todo.Count != 0)
            {
                var methodInfo = state.Todo.Dequeue();

                if (!state.HandledMethods.Add(methodInfo.Method)) // add it now to avoid StackOverflow for recursive methods
                    continue; // already handled

                if (settings.MaxDepth >= methodInfo.Depth)
                    result.Add(DisassembleMethod(methodInfo, state, settings));
            }

            return result.ToArray();
        }

        private static bool CanBeDisassembled(ClrMethod method)
            => !(method.ILOffsetMap.Length == 0 && (method.HotColdInfo.HotStart == 0 || method.HotColdInfo.HotSize == 0));

        private static DisassembledMethod DisassembleMethod(MethodInfo methodInfo, State state, Settings settings)
        {
            var method = methodInfo.Method;

            if (!CanBeDisassembled(method))
            {
                if (method.IsPInvoke)
                    return CreateEmpty(method, "PInvoke method");
                if (method.IL is null || method.IL.Length == 0)
                    return CreateEmpty(method, "Extern method");
                if (method.CompilationType == MethodCompilationType.None)
                    return CreateEmpty(method, "Method was not JITted yet.");

                return CreateEmpty(method, $"No valid {nameof(method.ILOffsetMap)} and {nameof(method.HotColdInfo)}");
            }

            var codes = new List<SourceCode>();
            if (settings.PrintSource && method.ILOffsetMap.Length > 0)
            {
                // we use HashSet to prevent from duplicates
                var uniqueSourceCodeLines = new HashSet<Sharp>(new SharpComparer());
                // for getting C# code we always use the original ILOffsetMap
                foreach (var map in method.ILOffsetMap.Where(map => map.StartAddress < map.EndAddress && map.ILOffset >= 0).OrderBy(map => map.StartAddress))
                    foreach (var sharp in SourceCodeProvider.GetSource(method, map))
                        uniqueSourceCodeLines.Add(sharp);

                codes.AddRange(uniqueSourceCodeLines);
            }

            // for getting ASM we try to use data from HotColdInfo if available (better for decoding)
            foreach (var map in GetCompleteNativeMap(method))
                codes.AddRange(Decode(map.StartAddress, (uint)(map.EndAddress - map.StartAddress), state, methodInfo.Depth, method));

            Map[] maps = settings.PrintSource
                ? codes.GroupBy(code => code.InstructionPointer).OrderBy(group => group.Key).Select(group => new Map() { SourceCodes = group.ToArray() }).ToArray()
                : new[] { new Map() { SourceCodes = codes.ToArray() } };

            return new DisassembledMethod
            {
                Maps = maps,
                Name = method.Signature,
                NativeCode = method.NativeCode
            };
        }

        private static IEnumerable<Asm> Decode(ulong startAddress, uint size, State state, int depth, ClrMethod currentMethod)
        {
            byte[] code = new byte[size];
            int bytesRead = state.Runtime.DataTarget.DataReader.Read(startAddress, code);
            if (bytesRead == 0 || bytesRead != size)
                yield break;

            var reader = new ByteArrayCodeReader(code, 0, bytesRead);
            var decoder = Decoder.Create(state.Runtime.DataTarget.DataReader.PointerSize * 8, reader);
            decoder.IP = startAddress;

            while (reader.CanReadByte)
            {
                decoder.Decode(out var instruction);

                TryTranslateAddressToName(instruction, state, depth, currentMethod);

                yield return new Asm
                {
                    InstructionPointer = instruction.IP,
                    Instruction = instruction
                };
            }
        }

        private static void TryTranslateAddressToName(Instruction instruction, State state, int depth, ClrMethod currentMethod)
        {
            var runtime = state.Runtime;

            if (!TryGetReferencedAddress(instruction, (uint)runtime.DataTarget.DataReader.PointerSize, out ulong address))
                return;

            if (state.AddressToNameMapping.ContainsKey(address))
                return;

            var jitHelperFunctionName = runtime.GetJitHelperFunctionName(address);
            if (!string.IsNullOrEmpty(jitHelperFunctionName))
            {
                state.AddressToNameMapping.Add(address, jitHelperFunctionName);
                return;
            }

            var methodTableName = runtime.DacLibrary.SOSDacInterface.GetMethodTableName(address);
            if (!string.IsNullOrEmpty(methodTableName))
            {
                state.AddressToNameMapping.Add(address, $"MT_{methodTableName}");
                return;
            }

            var methodDescriptor = runtime.GetMethodByHandle(address);
            if (!(methodDescriptor is null))
            {
                state.AddressToNameMapping.Add(address, $"MD_{methodDescriptor.Signature}");
                return;
            }

            var method = runtime.GetMethodByInstructionPointer(address);
            if (method is null && (address & ((uint)runtime.DataTarget.DataReader.PointerSize - 1)) == 0)
            {
                if (runtime.DataTarget.DataReader.ReadPointer(address, out ulong newAddress) && newAddress > ushort.MaxValue)
                    method = runtime.GetMethodByInstructionPointer(newAddress);
            }

            if (method is null)
                return;

            if (method.NativeCode == currentMethod.NativeCode && method.Signature == currentMethod.Signature)
                return; // in case of a call which is just a jump within the method or a recursive call

            if (!state.HandledMethods.Contains(method))
                state.Todo.Enqueue(new MethodInfo(method, depth + 1));

            var methodName = method.Signature;
            if (!methodName.Any(c => c == '.')) // the method name does not contain namespace and type name
                methodName = $"{method.Type.Name}.{method.Signature}";
            state.AddressToNameMapping.Add(address, methodName);
        }

        internal static bool TryGetReferencedAddress(Instruction instruction, uint pointerSize, out ulong referencedAddress)
        {
            for (int i = 0; i < instruction.OpCount; i++)
            {
                switch (instruction.GetOpKind(i))
                {
                    case OpKind.NearBranch16:
                    case OpKind.NearBranch32:
                    case OpKind.NearBranch64:
                        referencedAddress = instruction.NearBranchTarget;
                        return referencedAddress > ushort.MaxValue;
                    case OpKind.Immediate16:
                    case OpKind.Immediate8to16:
                    case OpKind.Immediate8to32:
                    case OpKind.Immediate8to64:
                    case OpKind.Immediate32to64:
                    case OpKind.Immediate32 when pointerSize == 4:
                    case OpKind.Immediate64:
                        referencedAddress = instruction.GetImmediate(i);
                        return referencedAddress > ushort.MaxValue;
                    case OpKind.Memory when instruction.IsIPRelativeMemoryOperand:
                        referencedAddress = instruction.IPRelativeMemoryAddress;
                        return referencedAddress > ushort.MaxValue;
                    case OpKind.Memory:
                        referencedAddress = instruction.MemoryDisplacement64;
                        return referencedAddress > ushort.MaxValue;
                }
            }

            referencedAddress = default;
            return false;
        }

        private static ILToNativeMap[] GetCompleteNativeMap(ClrMethod method)
        {
            // it's better to use one single map rather than few small ones
            // it's simply easier to get next instruction when decoding ;)
            var hotColdInfo = method.HotColdInfo;
            if (hotColdInfo.HotSize > 0 && hotColdInfo.HotStart > 0)
            {
                return hotColdInfo.ColdSize <= 0
                    ? new[] { new ILToNativeMap() { StartAddress = hotColdInfo.HotStart, EndAddress = hotColdInfo.HotStart + hotColdInfo.HotSize, ILOffset = -1 } }
                    : new[]
                      {
                            new ILToNativeMap() { StartAddress = hotColdInfo.HotStart, EndAddress = hotColdInfo.HotStart + hotColdInfo.HotSize, ILOffset = -1 },
                            new ILToNativeMap() { StartAddress = hotColdInfo.ColdStart, EndAddress = hotColdInfo.ColdStart + hotColdInfo.ColdSize, ILOffset = -1 }
                      };
            }

            return method.ILOffsetMap
                    .Where(map => map.StartAddress < map.EndAddress) // some maps have 0 length?
                    .OrderBy(map => map.StartAddress) // we need to print in the machine code order, not IL! #536
                    .ToArray();
        }

        private static DisassembledMethod CreateEmpty(ClrMethod method, string reason)
            => DisassembledMethod.Empty(method.Signature, method.NativeCode, reason);

        private class SharpComparer : IEqualityComparer<Sharp>
        {
            public bool Equals(Sharp x, Sharp y)
            {
                // sometimes some C# code lines are duplicated because the same line is the best match for multiple ILToNativeMaps
                // we don't want to confuse the users, so this must also be removed
                return x.FilePath == y.FilePath && x.LineNumber == y.LineNumber;
            }

            public int GetHashCode(Sharp obj) => obj.FilePath.GetHashCode() ^ obj.LineNumber;
        }
    }
}
