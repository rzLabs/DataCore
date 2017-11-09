using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace DataCore.Functions
{
    public class LUA
    {
        DynValue res;
        Script engine = null;
        private string scriptCode = null;

        public LUA(string scriptCode)
        {
            engine = new Script();
            res = engine.DoString(scriptCode);
            this.scriptCode = scriptCode;
        }

        public List<string> GetExtensions()
        {
            List<string> ret = new List<string>();
            try
            {
                Table t = (Table)engine.Globals["extensions"];
                for (int tIdx = 1; tIdx < t.Length + 1; tIdx++) { ret.Add(t.Get(tIdx).String); }
            }
            catch (SyntaxErrorException sEX) { throw new Exception(sEX.Message, sEX.InnerException); }

            return ret;
        }

        public List<string> GetUnencryptedExtensions()
        {
            List<string> ret = new List<string>();
            try
            {
                Table t = (Table)engine.Globals["unencrypted_extensions"];
                for (int tIdx = 1; tIdx < t.Length + 1; tIdx++) { ret.Add(t.Get(tIdx).String); }
            }
            catch (SyntaxErrorException sEX) { throw new Exception(sEX.Message, sEX.InnerException); }

            return ret;
        }

        public Dictionary<string, List<string>> GetGroupExports()
        {
            Dictionary<string, List<string>> ret = new Dictionary<string, List<string>>();
            try
            {
                Table t1 = (Table)engine.Globals["group_exports"];
                for (int tIdx = 1; tIdx < t1.Length + 1; tIdx++)
                {
                    Table t2 = (Table)t1.Get(tIdx).Table;

                    string groupName = t2.Get(1).String;
                    Table t3 = (Table)t2.Get(2).Table;
                    List<string> groupExtensions = new List<string>();
                    for (int t3Idx = 1; t3Idx < t3.Length + 1; t3Idx++)
                    {
                        groupExtensions.Add(t3.Get(t3Idx).String);
                    }

                    ret.Add(groupName, groupExtensions);
                }
            }
            catch (SyntaxErrorException sEX) { throw new Exception(sEX.Message, sEX.InnerException); }

            return ret;
        }
    }
}
