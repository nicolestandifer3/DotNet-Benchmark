﻿using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Helpers.Reflection.Emit;

namespace BenchmarkDotNet.Toolchains.InProcess.Emit.Implementation
{
    internal class ConsumableConsumeEmitter : ConsumeEmitter
    {
        private static MethodInfo GetConsumeMethod(Type consumableType)
        {
            var consumeMethod = typeof(Consumer).GetMethod(nameof(Consumer.Consume), new[] { consumableType });

            // Use generic method for ref types
            if (consumeMethod == null || consumeMethod.GetParameterTypes().FirstOrDefault() == typeof(object))
            {
                if (consumableType.IsClass || consumableType.IsInterface)
                {
                    consumeMethod = typeof(Consumer)
                        .GetMethods()
                        .Single(m =>
                        {
                            Type argType = m.GetParameterTypes().FirstOrDefault();

                            return m.Name == nameof(Consumer.Consume) && m.IsGenericMethodDefinition
                                && !argType.IsByRef // we are not interested in "Consume<T>(in T value)"
                                && argType.IsPointer == consumableType.IsPointer; // use "Consume<T>(T objectValue) where T : class" or "Consume<T>(T* ptrValue) where T: unmanaged"
                        });

                    consumeMethod = consumableType.IsPointer
                        ? consumeMethod.MakeGenericMethod(consumableType.GetElementType()) // consumableType is T*, we need T for Consume<T>(T* ptrValue)
                        : consumeMethod.MakeGenericMethod(consumableType);
                }
                else
                {
                    consumeMethod = null;
                }
            }

            if (consumeMethod == null)
            {
                throw new InvalidOperationException($"Cannot consume result of {consumableType}.");
            }

            return consumeMethod;
        }

        private FieldBuilder consumerField;
        private LocalBuilder disassemblyDiagnoserLocal;

        public ConsumableConsumeEmitter(ConsumableTypeInfo consumableTypeInfo) : base(consumableTypeInfo)
        {
        }

        protected override void OnDefineFieldsOverride(TypeBuilder runnableBuilder)
        {
            consumerField = runnableBuilder.DefineField(RunnableConstants.ConsumerFieldName, typeof(Consumer), FieldAttributes.Private);
        }

        protected override void DeclareDisassemblyDiagnoserLocalsOverride(ILGenerator ilBuilder)
        {
            // optional local if default(T) uses .initobj
            disassemblyDiagnoserLocal = ilBuilder.DeclareOptionalLocalForReturnDefault(ConsumableInfo.WorkloadMethodReturnType);
        }

        protected override void EmitDisassemblyDiagnoserReturnDefaultOverride(ILGenerator ilBuilder)
        {
            ilBuilder.EmitReturnDefault(ConsumableInfo.WorkloadMethodReturnType, disassemblyDiagnoserLocal);
        }

        protected override void OnEmitCtorBodyOverride(ConstructorBuilder constructorBuilder, ILGenerator ilBuilder)
        {
            var ctor = typeof(Consumer).GetConstructor(Array.Empty<Type>());
            if (ctor == null)
                throw new InvalidOperationException($"Cannot get default .ctor for {typeof(Consumer)}");

            /*
                // consumer = new Consumer();
                IL_0000: ldarg.0
                IL_0001: newobj instance void [BenchmarkDotNet]BenchmarkDotNet.Engines.Consumer::.ctor()
                IL_0006: stfld class [BenchmarkDotNet]BenchmarkDotNet.Engines.Consumer BenchmarkDotNet.Autogenerated.Runnable_0::consumer
             */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Newobj, ctor);
            ilBuilder.Emit(OpCodes.Stfld, consumerField);
        }

        protected override void EmitActionBeforeCallOverride(ILGenerator ilBuilder)
        {
            /*
                // consumer. ...;
                IL_000c: ldarg.0
                IL_000d: ldfld class [BenchmarkDotNet]BenchmarkDotNet.Engines.Consumer BenchmarkDotNet.Autogenerated.Runnable_0::consumer
             */
            ilBuilder.Emit(OpCodes.Ldarg_0);
            ilBuilder.Emit(OpCodes.Ldfld, consumerField);
        }

        protected override void EmitActionAfterCallOverride(ILGenerator ilBuilder)
        {
            /*
                // ... .Consume( ... )
                IL_001e: callvirt instance void [BenchmarkDotNet]BenchmarkDotNet.Engines.Consumer::Consume(string)
                -or-
                // ... .Consume( ... .ConsumableField);
                IL_001e: callvirt instance void [BenchmarkDotNet]BenchmarkDotNet.Engines.Consumer::Consume(int32)
                // -or- .Consume( ... );
                IL_001e: ldfld int32 BenchmarkDotNet.Samples.CustomWithConsumable::ConsumableField
                IL_0023: callvirt instance void [BenchmarkDotNet]BenchmarkDotNet.Engines.Consumer::Consume(int32)
             */
            if (ActionKind == RunnableActionKind.Overhead)
            {
                var overheadConsumeMethod = GetConsumeMethod(ConsumableInfo.OverheadMethodReturnType);
                ilBuilder.Emit(OpCodes.Callvirt, overheadConsumeMethod);
            }
            else
            {
                var consumeField = ConsumableInfo.WorkloadConsumableField;
                if (consumeField == null)
                {
                    var consumeMethod = GetConsumeMethod(ConsumableInfo.WorkloadMethodReturnType);
                    ilBuilder.Emit(OpCodes.Callvirt, consumeMethod);
                }
                else
                {
                    var consumeMethod = GetConsumeMethod(consumeField.FieldType);
                    ilBuilder.Emit(OpCodes.Ldfld, consumeField);
                    ilBuilder.Emit(OpCodes.Callvirt, consumeMethod);
                }
            }
        }
    }
}