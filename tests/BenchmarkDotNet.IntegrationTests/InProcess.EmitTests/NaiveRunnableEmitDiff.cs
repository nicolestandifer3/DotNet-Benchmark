﻿using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Loggers;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace BenchmarkDotNet.IntegrationTests.InProcess.EmitTests
{
    public class NaiveRunnableEmitDiff
    {
        private static readonly HashSet<string> IgnoredTypeNames = new HashSet<string>()
        {
            "BenchmarkDotNet.Autogenerated.UniqueProgramName"
        };

        private static readonly HashSet<string> IgnoredAttributeTypeNames = new HashSet<string>()
        {
            "System.Runtime.CompilerServices.CompilerGeneratedAttribute"
        };

        private static readonly HashSet<string> IgnoredRunnableMethodNames = new HashSet<string>()
        {
            "Run",
            ".ctor"
        };

        private static readonly IReadOnlyDictionary<OpCode, OpCode> AltOpCodes = new Dictionary<OpCode, OpCode>()
        {
            { OpCodes.Br_S, OpCodes.Br },
            { OpCodes.Blt_S, OpCodes.Blt },
            { OpCodes.Bne_Un_S, OpCodes.Bne_Un }
        };

        public static void RunDiff(string assemblyPath1, string assemblyPath2, ILogger logger)
        {
            using (var assemblyDefinition1 = AssemblyDefinition.ReadAssembly(assemblyPath1))
            using (var assemblyDefinition2 = AssemblyDefinition.ReadAssembly(assemblyPath2))
            {
                Diff(assemblyDefinition1, assemblyDefinition2, logger);
            }
        }

        private static bool IsRunnable(TypeReference t) =>
            t.FullName.StartsWith("BenchmarkDotNet.Autogenerated.Runnable_");

        private static bool AreSameTypeIgnoreNested(TypeReference type1, TypeReference type2)
        {
            if (type1 == null)
                return type2 == null;
            if (type2 == null)
                return false;

            return type1.FullName.Replace("/", "").Replace(".ReplaceMe.", ".") ==
                   type2.FullName.Replace("/", "").Replace(".ReplaceMe.", ".");
        }

        private static bool AreSameSignature(MethodReference m1, MethodReference m2)
        {
            return (m1.Name == m2.Name || (m1.Name.StartsWith("<.ctor>") && m2.Name == "__Workload"))
                && AreSameTypeIgnoreNested(m1.ReturnType, m2.ReturnType)
                && m1.Parameters.Count == m2.Parameters.Count
                && m1.Parameters
                    .Zip(m2.Parameters, (p1, p2) => (p1, p2))
                    .All(p => AreSameTypeIgnoreNested(p.p1.ParameterType, p.p2.ParameterType));
        }

        private static List<Instruction> GetOpInstructions(MethodDefinition method)
        {
            var bodyInstructions = method.Body.GetILProcessor().Body.Instructions;

            // There's something wrong with ldloc/ldarg with index >= 255. The c# compiler emits random nops for them.
            var compareNops = method.Body.Variables.Count < 255 && method.Parameters.Count < 255;
            var result = new List<Instruction>(bodyInstructions.Count);
            foreach (var instruction in bodyInstructions)
            {
                if (compareNops || instruction.OpCode != OpCodes.Nop)
                    result.Add(instruction);
            }

            return result;
        }

        private static void DiffSignature(TypeReference type1, TypeReference type2)
        {
            if (type2 == null)
                throw new InvalidOperationException($"No matching type for {type1}");

            if (!AreSameTypeIgnoreNested(type1, type2))
                throw new InvalidOperationException($"No matching type for {type1}");
        }

        private static void DiffSignature(FieldReference field1, FieldReference field2)
        {
            if (field2 == null)
                throw new InvalidOperationException($"No matching field for {field1.FullName}");

            if (!AreSameTypeIgnoreNested(field1.FieldType, field2.FieldType))
                throw new InvalidOperationException($"No matching field for {field1.FullName}");

            if (!AreSameTypeIgnoreNested(field1.DeclaringType, field2.DeclaringType))
                throw new InvalidOperationException($"No matching field for {field1.FullName}");
        }

        private static void DiffSignature(MethodReference method1, MethodReference method2)
        {
            if (method2 == null)
                throw new InvalidOperationException($"No matching method for {method1}");

            if (!AreSameSignature(method1, method2))
                throw new InvalidOperationException($"No matching method for {method1}");

            if (!AreSameTypeIgnoreNested(method1.DeclaringType, method2.DeclaringType))
                throw new InvalidOperationException($"No matching method for {method1}");
        }

        private static void DiffSignature(ParameterDefinition parameter1, ParameterDefinition parameter2)
        {
            if (parameter1.Name != parameter2.Name)
                throw new InvalidOperationException($"No matching parameter for {parameter1.Name} ({parameter1.Method})");

            if (!AreSameTypeIgnoreNested(parameter1.ParameterType, parameter2.ParameterType))
                throw new InvalidOperationException($"No matching parameter for {parameter1.Name} ({parameter1.Method})");

            if (parameter1.Attributes != parameter2.Attributes)
                throw new InvalidOperationException($"No matching parameter for {parameter1.Name} ({parameter1.Method})");
        }

        private static void DiffSignature(Instruction op1, Instruction op2, MethodDefinition method1)
        {
            if (op1.OpCode != op2.OpCode)
            {
                if (!AltOpCodes.TryGetValue(op1.OpCode, out var altOpCode1) || altOpCode1 != op2.OpCode)
                    throw new InvalidOperationException($"No matching op for {op1} ({method1}).");
            }
            else if (op1.GetSize() != op2.GetSize())
            {
                throw new InvalidOperationException($"No matching op for {op1} ({method1}).");
            }

            if (op1.Operand == null && op2.Operand != null)
                throw new InvalidOperationException($"No matching op for {op1} ({method1}).");

            if (op1.Operand != null && op2.Operand == null)
                throw new InvalidOperationException($"No matching op for {op1} ({method1}).");
        }

        private static void DiffSignature(VariableDefinition v1, VariableDefinition v2, MethodDefinition method1)
        {
            if (v1.Index != v2.Index)
                throw new InvalidOperationException($"No matching variable for {v1} ({method1}).");

            if (v1.IsPinned != v2.IsPinned)
                throw new InvalidOperationException($"No matching variable for {v1} ({method1}).");

            if (!AreSameTypeIgnoreNested(v1.VariableType, v2.VariableType))
                throw new InvalidOperationException($"No matching variable for {v1} ({method1}).");
        }

        private static void Diff(
            Collection<CustomAttribute> attributes1,
            Collection<CustomAttribute> attributes2,
            ICustomAttributeProvider owner1)
        {
            var attributes2ByTypeName = attributes2.ToLookup(a => a.AttributeType.FullName);
            foreach (var attribute1 in attributes1)
            {
                var attribute2 = attributes2ByTypeName[attribute1.AttributeType.FullName].FirstOrDefault();
                Diff(attribute1, attribute2, owner1);
            }
        }

        private static void Diff(CustomAttribute attribute1, CustomAttribute attribute2, ICustomAttributeProvider owner1)
        {

            if (IgnoredAttributeTypeNames.Contains(attribute1.AttributeType.FullName) && attribute2 == null)
                return;

            if (attribute2 == null)
                throw new InvalidOperationException($"No matching attribute for {attribute1.AttributeType} ({owner1})");

            if (!AreSameTypeIgnoreNested(attribute1.AttributeType, attribute2.AttributeType))
                throw new InvalidOperationException($"No matching attribute for {attribute1.AttributeType} ({owner1})");

            if (attribute1.ConstructorArguments.Count != attribute2.ConstructorArguments.Count)
                throw new InvalidOperationException($"No matching attribute for {attribute1.AttributeType} ({owner1})");

            for (int i = 0; i < attribute1.ConstructorArguments.Count; i++)
            {
                var attArg1 = attribute1.ConstructorArguments[i];
                var attArg2 = attribute2.ConstructorArguments[i];

                if (!AreSameTypeIgnoreNested(attArg1.Type, attArg2.Type))
                    throw new InvalidOperationException($"No matching attribute for {attribute1.AttributeType} ({owner1})");

                if (!Equals(attArg1.Value, attArg2.Value))
                    throw new InvalidOperationException($"No matching attribute for {attribute1.AttributeType} ({owner1})");
            }
        }

        private static void Diff(
            AssemblyDefinition assemblyDefinition1,
            AssemblyDefinition assemblyDefinition2,
            ILogger logger)
        {
            Diff(assemblyDefinition1.CustomAttributes, assemblyDefinition2.CustomAttributes, assemblyDefinition1);

            var modules2ByName = assemblyDefinition2.Modules.ToLookup(m => m.Name);
            foreach (var module1 in assemblyDefinition1.Modules)
            {
                var module2 = modules2ByName[module1.Name].SingleOrDefault();
                if (module2 == null && module1.IsMain)
                    module2 = assemblyDefinition2.MainModule;

                Diff(module1, module2, logger);
            }
        }

        private static void Diff(ModuleDefinition module1, ModuleDefinition module2, ILogger logger)
        {
            Diff(module1.CustomAttributes, module2.CustomAttributes, module1);

            foreach (var type1 in module1.Types)
            {
                var type2 = module2.Types
                    .SingleOrDefault(t => AreSameTypeIgnoreNested(type1, t));

                Diff(type1, type2, logger);
            }
        }

        private static void Diff(TypeDefinition type1, TypeDefinition type2, ILogger logger)
        {
            try
            {
                logger.WriteStatistic($"Diff {type1.FullName}");

                if (IgnoredTypeNames.Contains(type1.FullName) && type2 == null)
                {
                    logger.WriteLineInfo(" SKIPPED.");
                    return;
                }

                logger.WriteLine();

                DiffDefinition(type1, type2);

                DiffMembers(type1, type2, logger);
            }
            catch (Exception ex)
            {
                logger.WriteLineError(ex.ToString());
                throw;
            }
        }

        private static void DiffDefinition(TypeDefinition type1, TypeDefinition type2)
        {
            DiffSignature(type1, type2);

            if (!AreSameTypeIgnoreNested(type1.BaseType, type2.BaseType))
                throw new InvalidOperationException($"No matching type for {type1.FullName}");

            if (!AreSameTypeIgnoreNested(type1.DeclaringType, type2.DeclaringType))
                throw new InvalidOperationException($"No matching type for {type1.FullName}");

            if (type1.Attributes != type2.Attributes)
                throw new InvalidOperationException($"No matching type for {type1.FullName}");

            Diff(type1.CustomAttributes, type2.CustomAttributes, type1);
        }

        private static void DiffMembers(TypeDefinition type1, TypeDefinition type2, ILogger logger)
        {
            var fields2ByName = type2.Fields.ToLookup(f => f.Name);
            foreach (var field1 in type1.Fields)
            {
                logger.Write($"    field {field1.FullName}");

                var field2 = fields2ByName[field1.Name].SingleOrDefault();
                Diff(field1, field2);

                logger.WriteLineHelp(" OK.");
            }

            var methods2ByName = type2.Methods.ToLookup(f => f.Name);
            foreach (var method1 in type1.Methods)
            {
                logger.Write($"    method {method1.FullName}");

                var method2 = methods2ByName[method1.Name].SingleOrDefault(m => AreSameSignature(method1, m));
                if (method2 == null)
                    method2 = type2.Methods.SingleOrDefault(m => AreSameSignature(method1, m));
                if (method2 == null)
                    method2 = methods2ByName[method1.Name].SingleOrDefault();

                if (Diff(method1, method2))
                    logger.WriteLineHelp(" OK.");
                else
                    logger.WriteLineInfo(" SKIPPED.");
            }
        }

        private static void Diff(FieldDefinition field1, FieldDefinition field2)
        {
            DiffSignature(field1, field1);

            if (field1.Attributes != field2.Attributes)
                throw new InvalidOperationException($"No matching field for {field1.FullName}");

            Diff(field1.CustomAttributes, field2.CustomAttributes, field1);
        }

        private static bool Diff(MethodDefinition method1, MethodDefinition method2)
        {
            if (IsRunnable(method1.DeclaringType) && IgnoredRunnableMethodNames.Contains(method1.Name))
            {
                return false;
            }

            DiffDefinition(method1, method2);

            DiffVariables(method1, method2);

            DiffBody(method1, method2);

            return true;
        }

        private static void DiffDefinition(MethodDefinition method1, MethodDefinition method2)
        {
            DiffSignature(method1, method2);

            if (method1.Attributes != method2.Attributes)
                throw new InvalidOperationException($"No matching method for {method1}");

            if (method1.Parameters.Count != method2.Parameters.Count)
                throw new InvalidOperationException($"No matching method for {method1}");
            for (int i = 0; i < method1.Parameters.Count; i++)
            {
                var parameter1 = method1.Parameters[i];
                var parameter2 = method2.Parameters[i];
                Diff(parameter1, parameter2);
            }

            Diff(method1.MethodReturnType, method2.MethodReturnType);

            Diff(method1.CustomAttributes, method2.CustomAttributes, method1);
        }

        private static void Diff(ParameterDefinition parameter1, ParameterDefinition parameter2)
        {
            DiffSignature(parameter1, parameter2);

            Diff(parameter1.CustomAttributes, parameter2.CustomAttributes, parameter1);
        }

        private static void Diff(MethodReturnType returnType1, MethodReturnType returnType2)
        {
            if (!AreSameTypeIgnoreNested(returnType1.ReturnType, returnType2.ReturnType))
                throw new InvalidOperationException($"No matching method for {returnType1.Method}");

            if (returnType1.Attributes != returnType2.Attributes)
                throw new InvalidOperationException($"No matching method for {returnType1.Method}");

            Diff(returnType1.CustomAttributes, returnType2.CustomAttributes, returnType1);
        }

        private static void DiffVariables(MethodDefinition method1, MethodDefinition method2)
        {
            var variables1 = method1.Body.Variables.ToList();
            var variables2 = method2.Body.Variables.ToList();
            var diffMax = Math.Min(variables1.Count, variables2.Count);

            for (var i = 0; i < diffMax; i++)
            {
                DiffSignature(variables1[i], variables2[i], method1);
            }

            if (variables1.Count > diffMax)
                throw new InvalidOperationException($"There are additional variables in {method1}.");

            if (variables2.Count > diffMax)
                throw new InvalidOperationException($"There are additional variables in {method2}.");
        }

        private static void DiffBody(MethodDefinition method1, MethodDefinition method2)
        {
            var instructions1 = GetOpInstructions(method1);
            var instructions2 = GetOpInstructions(method2);
            var diffMax = Math.Min(instructions1.Count, instructions2.Count);

            var op2ToOp1Map = instructions1.Take(diffMax)
                .Zip(
                    instructions2.Take(diffMax),
                    (i1, i2) => (i1, i2))
                .ToDictionary(x => x.i2, x => x.i1);

            for (var i = 0; i < diffMax; i++)
            {
                Diff(instructions1[i], instructions2[i], method1, op2ToOp1Map);
            }

            if (instructions1.Count > diffMax)
                throw new InvalidOperationException($"There are additional instructions in {method1}.");

            if (instructions2.Count > diffMax)
                throw new InvalidOperationException($"There are additional instructions in {method2}.");
        }

        private static void Diff(Instruction op1, Instruction op2, MethodDefinition method1, Dictionary<Instruction, Instruction> op2ToOp1Map)
        {
            DiffSignature(op1, op2, method1);

            if (op1.Operand == null)
            {
                // Do nothing
            }
            else if (op1.Operand is TypeReference tr)
            {
                DiffSignature(tr, (TypeReference)op2.Operand);
            }
            else if (op1.Operand is FieldReference fr)
            {
                DiffSignature(fr, (FieldReference)op2.Operand);
            }
            else if (op1.Operand is MethodReference mr)
            {
                DiffSignature(mr, (MethodReference)op2.Operand);
            }
            else if (op1.Operand is ParameterDefinition p)
            {
                DiffSignature(p, (ParameterDefinition)op2.Operand);
            }
            else if (op1.Operand is VariableDefinition v)
            {
                DiffSignature(v, (VariableDefinition)op2.Operand, method1);
            }
            else if (op1.Operand is Instruction i)
            {
                op2ToOp1Map.TryGetValue((Instruction)op2.Operand, out var expectedOp1Operand);
                if (i != expectedOp1Operand)
                    throw new InvalidOperationException($"No matching op for {op1} ({method1}).");
            }
            else if (!Equals(op1.Operand, op2.Operand))
                throw new InvalidOperationException($"No matching op for {op1} ({method1}).");
        }
    }
}