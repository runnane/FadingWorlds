using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace fwlib
{
    public class Helper
    {
        private static readonly Random RandomCore = new Random();

        public static int Random(int low, int high)
        {
            return RandomCore.Next(low, high);
        }

        public static Vector2 GetVectorByPos(Position2D pos)
        {
            return new Vector2(pos.X * 32 + (32 / 2), pos.Y * 32 + (32 / 2));
        }

        public static string RandomString(int size)
        {
            lock (RandomCore)
            {
                StringBuilder builder = new StringBuilder();
                char ch;
                for (int i = 0; i < size; i++)
                {
                    ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * RandomCore.NextDouble() + 65)));
                    builder.Append(ch);
                }

                return builder.ToString();
            }
        }

        public static void ConsoleWrite(string s)
        {
            Console.WriteLine("[x]> " + s);
        }

        public static Dictionary<string, string> ConvertDataStringToDictionary(string inputstring)
        {
            Dictionary<string, string> output = new Dictionary<string, string>();
            List<string> attrs = inputstring.Split(',').ToList();
            foreach (string s in attrs)
            {
                string[] attrsplit = s.Split('=');
                string attr = attrsplit[0];
                string val = attrsplit[1];
                output.Add(attr, val);
            }
            return output;
        }

        public static string ReplaceDice(string input)
        {
            int[] dice = { 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24 };
            string result = input;
            foreach (int die in dice)
                result = result.Replace("D" + die, Random(1, die).ToString());
            return result;
        }

        public static int Evaluate(string expression)
        {

            expression = ReplaceDice(expression);
            var loDataTable = new DataTable();
            var loDataColumn = new DataColumn("Eval", typeof(double), expression);
            loDataTable.Columns.Add(loDataColumn);
            loDataTable.Rows.Add(0);
            string output = (loDataTable.Rows[0]["Eval"]).ToString();
            return int.Parse(output);
        }
    }
}
