﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PZPack.Test
{
    class ConsoleUpdater
    {
        private int cursorLeft;
        private int cursorTop;
        private bool Running = false;
        public bool IsRunning
        {
            get
            {
                return Running;
            }
        }

        public bool WriteInNewLine = true;
        private int lastEndLeft = 0;
        private int lastEndTop = 0;

        private void ClearLastUpdate()
        {
            if (lastEndLeft > 0 || lastEndTop > 0)
            {
                string emptyLine = new(' ', Console.BufferWidth);
                Console.SetCursorPosition(cursorLeft, cursorTop);
                int currLine = cursorTop;
                while (currLine <= lastEndTop)
                {
                    if (currLine == cursorTop)
                    {
                        Console.Write(new string(' ', Console.BufferWidth - cursorLeft));
                    }
                    else if (currLine == lastEndTop)
                    {
                        Console.Write(new string(' ', lastEndLeft));
                    }
                    else
                    {
                        Console.Write(emptyLine);
                    }
                    currLine++;
                }
            }
        }

        public void Begin()
        {
            if (Running)
            {
                return;
            }
            (int left, int top) = Console.GetCursorPosition();
            cursorLeft = left;
            cursorTop = top;

            Console.CursorVisible = false;
            Running = true;
        }
        public void Update(string text)
        {
            if (!Running)
            {
                return;
            }
            ClearLastUpdate();

            if (WriteInNewLine)
            {
                Console.SetCursorPosition(0, cursorTop + 1);
            }
            else
            {
                Console.SetCursorPosition(cursorLeft, cursorTop);
            }
            Console.Write(text);

            (int left, int top) = Console.GetCursorPosition();
            lastEndLeft = left;
            lastEndTop = top;
        }
        public void End()
        {
            if (!Running)
            {
                return;
            }
            ClearLastUpdate();
            lastEndLeft = 0;
            lastEndTop = 0;

            Console.SetCursorPosition(cursorLeft, cursorTop);
            Console.CursorVisible = true;
            Running = false;
        }
    }
}
