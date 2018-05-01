// Copyright (c) 2017-2018 Dover Microsystems, Inc.  All rights reserved.
// Use and disclosure subject to license. No claim made to open source code or materials.

using System;

namespace Antmicro.Renode.Plugins.ValidatorPlugin
{
    public interface IMetadataDebugger
    {
        String GetEnvMetadata();
        String GetRegMetadata(UInt64 addr);
        String GetAllRegMetadata();
        String GetCsrMetadata(UInt64 addr);
        String GetMemMetadata(UInt64 addr);
        void SetEnvMetadataWatch(bool watching);
        void SetRegMetadataWatch(UInt64 addr);
        void SetCsrMetadataWatch(UInt64 addr);
        void SetMemMetadataWatch(UInt64 addr);
        String PolicyViolationMsg();
    }
}
