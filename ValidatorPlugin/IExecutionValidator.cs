// Copyright (c) 2017-2018 Dover Microsystems, Inc.  All rights reserved.
// Use and disclosure subject to license. No claim made to open source code or materials.

using System;

namespace Antmicro.Renode.Plugins.ValidatorPlugin
{
    public delegate UInt32 RegisterReader(int regno);
    public delegate UInt32 MemoryReader(long address);

    public interface IExecutionValidator
    {
        void SetCallbacks(RegisterReader RegReader, MemoryReader MemReader);
        bool Validate(uint PC, uint InstructionBits);
        bool Commit();
    }
}
