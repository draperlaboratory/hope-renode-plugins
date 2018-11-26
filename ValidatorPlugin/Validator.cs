using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.CPU;
using Antmicro.Renode.Utilities;

namespace Antmicro.Renode.Plugins.ValidatorPlugin
{
    public static class ValidatorExtensions
    {
        public static void SetExternalValidator(this TranslationCPU cpu, string so_file, string validator_cfg_path)
        {
            /*
             *  Validators can only function correctly if they are called for every
             *  instruction.  In order to do that, we have to set the block size to 1,
             *  so that tlib will not execute arbitrary sized basic blocks atomically
             *  from the perspective of Renode.
            */
            Validator.Instance.SetExternalValidator(cpu, so_file, validator_cfg_path);
        }

        public static String EnvMetadata(this TranslationCPU cpu)
        {
            if(Validator.MetaDebugger == null)
                return noValidatorErrorMsg;
            else
                return Validator.MetaDebugger.GetEnvMetadata();
        } 
        
        public static String RegMetadata(this TranslationCPU cpu, UInt64 addr)
        {
            if(Validator.MetaDebugger == null)
                return noValidatorErrorMsg;
            else
                return Validator.MetaDebugger.GetRegMetadata(addr);
        } 
        
        public static String AllRegMetadata(this TranslationCPU cpu)
        {
            if(Validator.MetaDebugger == null)
                return noValidatorErrorMsg;
            else
                return Validator.MetaDebugger.GetAllRegMetadata();
        } 
        
        public static String CsrMetadata(this TranslationCPU cpu, UInt64 addr)
        {
            if(Validator.MetaDebugger == null)
                return noValidatorErrorMsg;
            else
                return Validator.MetaDebugger.GetCsrMetadata(addr);
        } 
        
        public static String MemMetadata(this TranslationCPU cpu, UInt64 addr)
        {
            if(Validator.MetaDebugger == null)
                return noValidatorErrorMsg;
            else
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
            if(Validator.MetaDebugger == null)
                return noValidatorErrorMsg;
            else
                return Validator.MetaDebugger.PolicyViolationMsg();
        } 

        public static String RuleEvalLog(this TranslationCPU cpu)
        {
            if(Validator.MetaDebugger == null)
                return noValidatorErrorMsg;
            else
                return Validator.MetaDebugger.RuleEvalLog();
        } 

        /* Turn on simulator performance status messages */
        public static void SimPerformance(this TranslationCPU cpu)
        {
            Validator.SimPerformance = true;;
        } 
        
        public static void ValidatorStatusLogger(this TranslationCPU cpu, string path)
        {
            Validator.Instance.ValidatorStatusLogger(path);
        } 

        /*
         * Set metadata logging level for messages sent out the status server port
         * Levels:
         *         0 : No logging, watchpoints work normally
         *         1 : Long log message when watchpoint hit, but don't stop for watchpoints
         *         2 : Short log message on all instructions, watchpoints work normally
         *         3 : Long log message on all instructions, watchpoints work normally
         */
        public static void SetMetaLogLevel(this TranslationCPU cpu, int level)
        {
            Validator.MetaLogLevel = level;
        } 

        private static String noValidatorErrorMsg = "No Validator installed";
    }

    public class Validator
    {
        private Validator()
        {
            MetaLogLevel = 0;
        }

        static Validator() => validator = new Validator();

        public static Validator Instance => validator;
        public static IMetadataDebugger MetaDebugger => metaDebugger;
        public static bool SimPerformance = false;
        public static int MetaLogLevel {get; set;}
        private static ulong lastAddress;
        
        public void BlockBeginHook(ulong address, uint blockLength)
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
                    bool hitWatch;
                    hitWatch = executionValidator.Commit();
                    
                    //cpu.Log(LogLevel.Warning, "Commit: {0:X}", lastAddress);
                    if(hitWatch && (MetaLogLevel != 1))
                    {
                        cpu.Log(LogLevel.Info, "Validator Watchpoint Hit: 0x{0:X}", lastAddress);
                        //cpu.EnterSingleStepModeSafely(new HaltArguments(HaltReason.Step, address, BreakpointType.AccessWatchpoint));

                          using(cpu.ObtainPauseGuard(true, address))
                          {
                          cpu.Pause();
                          }

                        //cpu.ReportAbort("Validator requested pause");
                    }
                    // Some logic to implement the levels described above
                    if(MetaLogLevel > 1 || hitWatch){
                        if(MetaLogLevel > 0)
                            SendStatusMessage(metaDebugger.MetaLog(lastAddress));
                        if((MetaLogLevel == 1) || (MetaLogLevel > 2))
                            SendStatusMessage(metaDebugger.MetaLogDetail());
                    }
                    commitPending = false;
                }


                if(commitPending && lastAddress != address)

                {
                    cpu.Log(LogLevel.Error, "Validator skipped commit: 0x{0:X}", lastAddress);

                }
                if(!executionValidator.Validate((uint)address, cpu.Bus.ReadDoubleWord(address)))
                {
                    cpu.Log(LogLevel.Info, "Validator Vaidation Failed: 0x{0:X}", address);
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
                    lastAddress = address;
                    //cpu.Log(LogLevel.Warning, "Validate: {0:X}", lastAddress);
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

        public void SetExternalValidator(TranslationCPU cpu, string so_file, string validator_cfg_path)
        {
            ExternalValidator ev = new ExternalValidator(so_file, validator_cfg_path);
            
            executionValidator = ev;
            metaDebugger = ev;
            
            regReader = (int regno) => {return cpu.GetRegisterUnsafe(regno);};
            memReader = (ulong address) => {return cpu.Bus.ReadDoubleWord(address);};
            executionValidator.SetCallbacks(regReader, memReader);

            this.cpu = cpu;
            cpu.MaximumBlockSize = 1;

            cpu.SetHookAtBlockBegin(BlockBeginHook);
        }

        public void ValidatorStatusLogger(string path)
        {
            stream = new StreamWriter(path);
            cpu.Log(LogLevel.Info, "Logging validator status to {0}", path);
        }

        private void SendStatusMessage(String msg)
        {
            if(stream != null)
            {
		stream.Write(msg);
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
        private static StreamWriter stream;
    }
}
