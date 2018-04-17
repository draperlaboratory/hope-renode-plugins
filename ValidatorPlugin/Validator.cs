using System;
using System.Text;
using System.Diagnostics;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.CPU;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Plugins.ValidatorPlugin
{
    public static class ValidatorExtensions
    {
        public static void SetExternalValidator(this TranslationCPU cpu, string so_file, string pol_path, string taginfo_file )
        {
            /*
             *  Validators can only function correctly if they are called for every
             *  instruction.  In order to do that, we have to set the block size to 1,
             *  so that tlib will not execute arbitrary sized basic blocks atomically
             *  from the perspective of Renode.
            */
            Validator.Instance.SetExternalValidator(cpu, so_file, pol_path, taginfo_file);
        }

        public static String EnvMetadata(this TranslationCPU cpu)
        {
            return Validator.MetaDebugger.GetEnvMetadata();
        } 
        
        public static String RegMetadata(this TranslationCPU cpu, UInt64 addr)
        {
            return Validator.MetaDebugger.GetRegMetadata(addr);
        } 
        
        public static String CsrMetadata(this TranslationCPU cpu, UInt64 addr)
        {
            return Validator.MetaDebugger.GetCsrMetadata(addr);
        } 
        
        public static String MemMetadata(this TranslationCPU cpu, UInt64 addr)
        {
            return Validator.MetaDebugger.GetMemMetadata(addr);
        } 


        public static void EnvMetadataWatch(this TranslationCPU cpu, bool watching)
        {
            Validator.MetaDebugger.SetEnvMetadataWatch(watching);
        }

        public static void RegMetadataWatch(this TranslationCPU cpu, UInt64 addr)
        {
            Validator.MetaDebugger.SetRegMetadataWatch(addr);
        }

        public static void CsrMetadataWatch(this TranslationCPU cpu, UInt64 addr)
        {
            Validator.MetaDebugger.SetCsrMetadataWatch(addr);
        }

        public static void MemMetadataWatch(this TranslationCPU cpu, UInt64 addr)
        {
            Validator.MetaDebugger.SetMemMetadataWatch(addr);
        }

        public static String PolicyViolationMsg(this TranslationCPU cpu)
        {
            return Validator.MetaDebugger.PolicyViolationMsg();
        } 

        /* Turn on simulator performance status messages */
        public static void SimPerformance(this TranslationCPU cpu)
        {
            Validator.SimPerformance = true;;
        } 
        
        public static void StartStatusServer(this TranslationCPU cpu, int port)
        {
            Validator.Instance.StartStatusServer(port);
        } 
        
    }

    public class Validator
    {
        private Validator()
        {
        }

        static Validator() => validator = new Validator();

        public static Validator Instance => validator;
        public static IMetadataDebugger MetaDebugger => metaDebugger;
        public static bool SimPerformance = false;
        
        public void BlockBeginHook(uint address, uint blockLength)
        {
            if(executionValidator != null)
            {
                blockCount++;

                if(SimPerformance && !stopWatch.IsRunning)
                {
                    stopWatch.Start();
                }
            
                if(cpu.BlockCompleted() && commitPending)
                {
                    if(executionValidator.Commit())
                    {
                        cpu.Log(LogLevel.Info, "Validator Watchpoint Hit");
                        //cpu.EnterSingleStepModeSafely(new HaltArguments(HaltReason.Step, address, BreakpointType.AccessWatchpoint));

                          using(cpu.ObtainPauseGuard(true, address))
                          {
                          cpu.Pause();
                          }

                        //cpu.ReportAbort("Validator requested pause");
                    }
                }

                commitPending = false;
            
                if(!executionValidator.Validate(address, cpu.Bus.ReadDoubleWord(address)))
                {
                    cpu.Log(LogLevel.Info, "Validator Vaidation Failed");
                    //cpu.EnterSingleStepModeSafely(new HaltArguments(HaltReason.Step, address, BreakpointType.AccessWatchpoint));

                      using(cpu.ObtainPauseGuard(true, address))
                      {
                      cpu.Pause();
                      }

                      SendStatusMessage(cpu.PolicyViolationMsg());
                      SendStatusMessage("MSG: End test.\n");

                    //cpu.ReportAbort("Policy violation");
                }
                else
                {
                    commitPending = true;
                }

                if(SimPerformance && stopWatch.ElapsedMilliseconds >= 1000)
                {
                    cpu.Log(LogLevel.Info, "block count {0} ms = {1}.", blockCount, stopWatch.ElapsedMilliseconds);
                    blockCount = 0;
                    stopWatch.Reset();
                    stopWatch.Start();
                }
            }
        }

        public void SetExternalValidator(TranslationCPU cpu, string so_file, string pol_path, string taginfo_file )
        {
            ExternalValidator ev = new ExternalValidator(so_file, pol_path, taginfo_file );
            
            executionValidator = ev;
            metaDebugger = ev;
            
            regReader = (int regno) => {return cpu.GetRegisterUnsafe(regno);};
            memReader = (long address) => {return cpu.Bus.ReadDoubleWord(address);};
            executionValidator.SetCallbacks(regReader, memReader);

            this.cpu = cpu;
            cpu.MaximumBlockSize = 1;
            cpu.SetHookAtBlockBegin(BlockBeginHook);
        }

        public void StartStatusServer(int port)
        {
            validatorStatusServer = new SocketServerProvider();
            validatorStatusServer.Start(port);
            cpu.Log(LogLevel.Info, "Starting Validator Status Server on port: {0}", port);
        }

        private void SendStatusMessage(String msg)
        {
            if(validatorStatusServer != null)
            {
                Byte[] bytes = Encoding.ASCII.GetBytes(msg);
                foreach(var b in bytes)
                {
                    validatorStatusServer.SendByte(b);
                }
            }

        }
        
        private RegisterReader regReader;
        
        private MemoryReader memReader;


        
        private TranslationCPU cpu;
        private bool commitPending;

        private Stopwatch stopWatch = new Stopwatch();
        private ulong blockCount;
        private IExecutionValidator executionValidator;

        private static Validator validator;
        private static IMetadataDebugger metaDebugger;
        private static SocketServerProvider validatorStatusServer;
    }
}
