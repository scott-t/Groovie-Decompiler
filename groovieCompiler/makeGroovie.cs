using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace groovieCompiler
{
    class makeGroovie
    {
        System.IO.BinaryWriter grvWriter;
        System.IO.StreamReader asmReader;

        System.Collections.Generic.Dictionary<string, long> labelMap;
        System.Collections.Generic.Dictionary<long, string> labelsToSub;

        int lineNum = 0;

        static void Main(string[] args)
        {
            System.Console.WriteLine("------------------------------------");
            System.Console.WriteLine("-  Groovie Script (Dis-)Assembler  -");
            System.Console.WriteLine("------------------------------------\n");
            if (args.Length == 0 || args.Length > 2 || (args.Length == 2 && (args[0] != "-d" && args[0] != "-d2")))
            {
                System.Console.WriteLine("Usage: groovieCompiler <input file>\n  Compiles <input file> into the equivalent Groovie script file \"input.grv\"\n");
                System.Console.WriteLine("Usage: groovieCompiler -d[2] <input file>\n  Attempts to decompile <input file> into a Groovie-ASM file \"input.gasm\"\n");
                return;
            }
            bool t7g = true;
            if (args[0] == "-d2")
                t7g = false;

            if (args.Length == 1)
                new makeGroovie(args[0]);
            else
                new disGroovie(args[1], t7g);

            System.Console.WriteLine("\nFinished - Press any key to continue");
            System.Console.ReadKey(true);
            //System.Console.In.ReadLine();

        }

        public makeGroovie(string input)
        {
            try
            {
                asmReader = new System.IO.StreamReader(input);
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Unable to open file '" + input + "': ");
                System.Console.WriteLine(e.Message);
                return;
            }

            try
            {
                if (input.Contains("."))
                {
                    input = input.Substring(0, input.LastIndexOf("."));
                }
                input += ".grv";
                grvWriter = new System.IO.BinaryWriter(new System.IO.FileStream(input, System.IO.FileMode.Create, System.IO.FileAccess.Write));
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Unable to create output '" + input + "':");
                System.Console.WriteLine(e.Message);
                asmReader.Close();
                return;
            }

            labelMap = new Dictionary<string, long>();
            labelsToSub = new Dictionary<long, string>();

            string line;
            string[] parts;
            lineNum = 0;
            while (!asmReader.EndOfStream)
            {
                lineNum++;
                line = asmReader.ReadLine();
                if (line.Contains(";"))
                    line = line.Substring(0, line.IndexOf(";"));

                line = ":" + line;

                parts = line.Split(new char[] { '\t', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts[0].Length > 0)
                {
                    if (!parts[0].Equals(":"))
                    {
                        try
                        {
                            labelMap.Add(parts[0].Substring(1, parts[0].Length - 1), grvWriter.BaseStream.Position);
                        }
                        catch
                        {
                            parseError("Duplicate label name detected: " + parts[0].Substring(1, parts[0].Length - 1));
                            return;
                        }
                    }
                }

                if (parts.Length > 1)
                {
                    if (!parseOpcode(parts))
                        return;
                }
            }

            asmReader.Close();

            lineNum = -1;
            foreach (long pos in labelsToSub.Keys)
            {
                string lbl = labelsToSub[pos];
                if (!labelMap.ContainsKey(lbl))
                {
                    parseError("Unknown label name: " + lbl);
                    return;
                }

                grvWriter.BaseStream.Position = pos;
                grvWriter.Write((ushort)labelMap[lbl]);
            }

            grvWriter.Close();
        }

        private void parseError(string msg)
        {
            if (lineNum == -1)
                System.Console.WriteLine("Syntax error on unspecified line: " + msg);
            else
                System.Console.WriteLine("Syntax error on line " + lineNum + ": " + msg);
        }

        private bool tryParse(string s, out int result)
        {
            int n;
            if (int.TryParse(s, out n))
            {
                result = n;
                return true;
            }
            else
            {
                try
                {
                    n = Convert.ToInt32(s, 16);
                }
                catch
                {
                    result = 0;
                    return false;
                }
                result = n;
                return true;
            }
        }

        private void writeByte(string num, int offset)
        {
            int n;
            if (tryParse(num, out n))
                writeByte(n + offset);
            else
                throw new ParseException("Invalid parameter \'" + num + "\' - expected uint8");
        }

        private void writeByte(int num)
        {
            if (num > 255 || num < 0)
                parseError("Invalid parameter \'" + num.ToString() + "\' - expected uint8");

            grvWriter.Write((byte)num);
        }

        private void writeByte(string num)
        {
            int n;
            if (tryParse(num, out n))
                writeByte(n);
            else
                throw new ParseException("Invalid parameter \'" + num + "\' - expected uint8");
        }

        private void writeUShort(string num)
        {
            int n;
            if (tryParse(num, out n))
            {
                if (n >= ushort.MinValue && n <= ushort.MaxValue)
                {
                    grvWriter.Write((ushort)n);
                    return;
                }
            }

            throw new ParseException("Invalid parameter \'" + num + "\' - expected uint16");
        }

        private void writeLabel(string lbl)
        {
            labelsToSub.Add(grvWriter.BaseStream.Position, lbl);
            grvWriter.Write((ushort)0);
        }

        private bool parseOpcode(string[] line)
        {
            try
            {
                switch (line[1])
                {
                    case "ADD":
                        writeByte(0x25);
                        writeUShort(line[2]);
                        writeUShort(line[3]);
                        break;

                    case "BF5ON":
                        writeByte(0x0A);
                        break;

                    case "BF6ON":
                        writeByte(0x06);
                        break;

                    case "BF7ON":
                        writeByte(0x07);
                        break;

                    case "BF7OFF":
                        writeByte(0x35);
                        break;

                    case "BF8ON":
                        writeByte(0x05);
                        break;

                    case "BG2FG":
                        writeByte(0x22);
                        break;

                    case "CALL":
                        writeByte(0x18);
                        writeLabel(line[2]);
                        break;

                    case "CCD":
                        writeByte(0x4C);
                        break;

                    case "CLRVARS":
                        writeByte(0x3D);
                        break;

                    case "CSAVE":
                        writeByte(0x3C);
                        break;

                    case "DEC":
                        writeByte(0x20);
                        writeUShort(line[2]);
                        break;


                    case "EXIT":
                        writeByte(0x2A);
                        break;

                    case "FADOUT":
                        writeByte(0x04);
                        break;

                    case "FADIN":
                        writeByte(0x03);
                        break;

                    case "REC2BG":
                        writeByte(0x37);
                        writeUShort(line[2]);
                        writeUShort(line[3]);
                        writeUShort(line[4]);
                        writeUShort(line[5]); //left, top, right, bottom
                        break;

                    case "HOTB":
                        writeByte(0x30);
                        writeLabel(line[2]);
                        break;

                    case "HOTC":
                        writeByte(0x10);
                        writeLabel(line[2]);
                        break;

                    case "HOTFULL":
                        writeByte(0x12);
                        writeLabel(line[2]);
                        break;

                    case "HOTKEY":
                        writeByte(0x0C);
                        writeLabel(line[2]);
                        writeByte(line[3]);
                        break;

                    case "HOTL":
                        writeByte(0x0E);
                        writeLabel(line[2]);
                        break;

                    case "HOTLD":               // left dest (use current cursor)
                        writeByte(0x45);
                        writeLabel(line[2]);
                        break;

                    case "HOTR":
                        writeByte(0x0F);
                        writeLabel(line[2]);
                        break;

                    case "HOTRD":               // right dest (use current cursor)
                        writeByte(0x44);
                        writeLabel(line[2]);
                        break;

                    case "HOTREC":
                        writeByte(0x0D);
                        writeUShort(line[3]);
                        writeUShort(line[4]);
                        writeUShort(line[5]);
                        writeUShort(line[6]);
                        writeLabel(line[2]);
                        writeByte(line[7]);
                        break;

                    case "HOTSAV":
                        writeByte(0x3D);
                        writeByte(line[2]);
                        writeUShort(line[4]);
                        writeUShort(line[5]);
                        writeUShort(line[6]);
                        writeUShort(line[7]);
                        writeLabel(line[3]);
                        writeByte(line[8]);
                        break;

                    case "HOTTC":               // top with cursor
                        writeByte(0x2C);
                        writeLabel(line[2]);
                        writeByte(line[3]);
                        break;

                    case "HOTBC":               // bottom with cursor
                        writeByte(0x2D);
                        writeLabel(line[2]);
                        writeByte(line[3]);
                        break;

                    case "GOIN":
                        writeByte(0x0B);
                        break;
                    case "STIN":        //STop INput
                        writeByte(0x13);
                        break;

                    case "INC":
                        writeByte(0x1F);
                        writeUShort(line[2]);
                        break;

                    case "JNE":    //string JNE
                        if (line.Length < 5)
                            throw new ArgumentOutOfRangeException();

                        writeByte(0x1A);
                        writeUShort(line[3]);
                        writeAwk1(line, 4);
                        writeLabel(line[2]);
                        break;

                    case "JE":
                        if (line.Length < 5)
                            throw new ArgumentOutOfRangeException();

                        writeByte(0x23);
                        writeUShort(line[3]);
                        writeAwk1(line, 4);
                        writeLabel(line[2]);
                        break;

                    case "JG":
                        if (line.Length < 5)
                            throw new ArgumentOutOfRangeException();
                        writeByte(0x34);
                        writeUShort(line[3]);
                        writeAwk1(line, 4);
                        writeLabel(line[2]);
                        break;

                    case "JL":
                        if (line.Length < 5)
                            throw new ArgumentOutOfRangeException();
                        writeByte(0x36);
                        writeUShort(line[3]);
                        writeAwk1(line, 4);
                        writeLabel(line[2]);
                        break;

                    case "JMP":
                        writeByte(0x15);
                        writeLabel(line[2]);
                        break;

                    case "LOAD":
                        writeByte(0x2E);
                        writeUShort(line[2]);
                        break;

                    case "MIDLOOP":
                        writeByte(0x08);
                        writeUShort(line[2]);
                        break;

                    case "MIDVOL":
                        writeByte(0x31);
                        writeUShort(line[2]);
                        writeUShort(line[3]);
                        break;

                    case "MOD":
                        writeByte(0x3E);
                        writeUShort(line[2]);
                        writeByte(line[3]);
                        break;

                    case "MOV":
                        writeByte(0x24);
                        writeUShort(line[2]);
                        writeUShort(line[3]);
                        break;

                    case "MOVS":
                        writeByte(0x16);
                        if (line.Length < 4)
                            throw new ArgumentOutOfRangeException();
                        writeUShort(line[2]);
                        writeAwk1(line, 3);
                        break;

                    case "NOP":
                        writeByte(0x01);
                        break;

                    case "PMID":
                        writeByte(0x02);
                        writeUShort(line[2]);
                        break;

                    case "PRINT":
                        writeByte(0x3A);
                        writeAwk1(line, 2);
                        break;

                    case "PVDX":
                        writeByte(0x09);
                        writeUShort(line[2]);
                        break;

                    case "PVDX2":
                        writeByte(0x1C);
                        writeUShort(line[2]);
                        break;

                    case "PCD":
                        writeByte(0x4D);
                        writeByte(line[2]);
                        break;

                    case "RAND":
                        writeByte(0x14);
                        writeUShort(line[2]);
                        writeByte(line[3]);
                        break;

                    case "RET":
                        writeByte(0x17);
                        writeByte(line[2]);
                        break;

                    case "RETFS":
                        writeByte(0x43);
                        writeByte(line[3]);
                        break;

                    case "RLD":
                        writeByte(0x38);
                        break;

                    case "SAVE":
                        writeByte(0x2F);
                        writeUShort(line[2]);
                        break;

                    case "SLEEP":
                        writeByte(0x19);
                        writeUShort(line[2]);
                        break;

                    case "SMID":
                        writeByte(0x29);
                        break;

                    case "SUB":
                        writeByte(0x41);
                        writeUShort(line[2]);
                        writeUShort(line[3]);
                        break;

                    case "SWAP":
                        writeByte(0x1D);
                        writeUShort(line[2]);
                        writeUShort(line[3]);
                        break;

                    case "XOR":
                        writeByte(0x1B);
                        writeUShort(line[2]);
                        if (line.Length < 4)
                            throw new ArgumentOutOfRangeException();

                        for (int i = 3; i < line.Length - 1; i++)
                        {
                            if (i == line.Length - 1)
                                writeByte(line[i], 0x80);
                            else
                                writeByte(line[i]);
                        }
                        break;

                    default:
                        parseError("Invalid opcode \'" + line[1] + "\'");
                        return false;
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                parseError("Not enough parameters for opcode \'" + line[1] + "\'");
                System.Diagnostics.Debug.Write("ArgOutOfRange: " + ex.Message + "; " + ex.Source);
                return false;
            }
            catch (ParseException ex)
            {
                parseError("Parser exception: \'" + ex.Message + "\'");
                return false;
            }
            catch (Exception ex)
            {
                parseError("Unknown parse error: " + ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Convert GASM awkward read 1 into bytes
        /// </summary>
        /// <param name="line"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        private void writeAwk1(string[] line, int seed)
        {

            if (line[seed].ToLower() == "Arr1D")
            {
                for (int i = seed + 1; i < line.Length - 1; i++){
                    writeByte(0x23);
                    if (i == line.Length - 1)
                    {
                        writeByte(line[i], 0x61 + 0x80);
                    }
                    else
                    {
                        writeByte(line[i], 0x61);
                    }
                }
            }
            else if (line[seed].ToLower() == "Arr2D")
            {
                throw new ParseException("2D array fail");
            }
            else
            {
                for (int i = seed; i < line.Length - 1; i++)
                {
                    if (i == line.Length - 1)
                        writeByte(line[i], 0xB0);
                    else
                        writeByte(line[i], 0x30);
                }
            }
        }

        //if (data == 0x23)
        //        {
        //            ret = "Arr1D";

        //            grvReader.BaseStream.Seek(-1, System.IO.SeekOrigin.Current);
        //            while (grvReader.ReadByte() == 0x23)
        //            {
        //                data = grvReader.ReadByte();
        //                ret += ", " + (byte)((data & 0x7f) - 0x61);
        //            }

        //            if ((data & 0x80) == 0)
        //            {
        //                Console.WriteLine("Mass failure on 1D array reading");
        //            }

        //            //byte data2 = (byte)(grvReader.ReadByte() & 0x7f);

        //        }
        //        else if (data == 0x7C)
        //        {
        //            // 2D array
        //            byte data2, data3;
        //            data2 = grvReader.ReadByte();
        //            if (data2 == 0x23)
        //            {
        //                data2 -= 0x61;
        //                //data2 = vars[data2]; 
        //            }
        //            else
        //            {
        //                //data2 = vars[data2 - 0x30];
        //            }
        //        }
        //        else
        //        {
        //            // simple read
        //            data -= 0x30;
        //        }

        //        return ret;
        //    }
        //}

        class ParseException
            : Exception
        {
            private string _msg;
            public ParseException()
            {
                _msg = "A ParseException was thrown";
            }

            public ParseException(string msg)
            {
                _msg = msg;
            }

            public override string Message
            {
                get
                {
                    return _msg;
                }
            }
        }
    }
}
