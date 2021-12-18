namespace PZPack.Cli
{
    class ConsoleHelper
    {
        public static int MultipleChoice(bool canCancel, params string[] options)
        {
            const int startX = 15;
            const int startY = 8;
            const int optionsPerLine = 3;
            const int spacingPerLine = 14;

            int currentSelection = 0;

            ConsoleKey key;

            Console.CursorVisible = false;

            do
            {
                Console.Clear();

                for (int i = 0; i < options.Length; i++)
                {
                    Console.SetCursorPosition(startX + (i % optionsPerLine) * spacingPerLine, startY + i / optionsPerLine);

                    if (i == currentSelection)
                        Console.ForegroundColor = ConsoleColor.Red;

                    Console.Write(options[i]);

                    Console.ResetColor();
                }

                key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                        {
                            if (currentSelection % optionsPerLine > 0)
                                currentSelection--;
                            break;
                        }
                    case ConsoleKey.RightArrow:
                        {
                            if (currentSelection % optionsPerLine < optionsPerLine - 1)
                                currentSelection++;
                            break;
                        }
                    case ConsoleKey.UpArrow:
                        {
                            if (currentSelection >= optionsPerLine)
                                currentSelection -= optionsPerLine;
                            break;
                        }
                    case ConsoleKey.DownArrow:
                        {
                            if (currentSelection + optionsPerLine < options.Length)
                                currentSelection += optionsPerLine;
                            break;
                        }
                    case ConsoleKey.Escape:
                        {
                            if (canCancel)
                                return -1;
                            break;
                        }
                }
            } while (key != ConsoleKey.Enter);

            Console.CursorVisible = true;

            return currentSelection;
        }

        public static int ConsoleSelect(string[] options)
        {
            var (Left, Top) = Console.GetCursorPosition();
            (int Left, int Top) endPos;
            Console.CursorVisible = false;

            int selected = 0;
            ConsoleKey key;
            string emptyline = new(' ', Console.BufferWidth);

            int writeTop = Left == 0 ? Top : Top + 1;
            do
            {
                Console.SetCursorPosition(0, writeTop);

                for (int i = 0; i < options.Length; i++)
                {
                    Console.WriteLine($"{(selected == i ? ">" : " ")}{i + 1}. {options[i]}");
                }

                key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        if (selected > 0) selected--;
                        break;
                    case ConsoleKey.DownArrow:
                        if (selected < options.Length - 1) selected++;
                        break;
                }

                endPos = Console.GetCursorPosition();
                int coi = Top + 1;
                while (coi < endPos.Top)
                {
                    Console.SetCursorPosition(0, coi);
                    Console.WriteLine(emptyline);
                    coi++;
                }
            } while (key != ConsoleKey.Enter);

            Console.SetCursorPosition(Left, Top);
            Console.CursorVisible = true;

            return selected;
        }
    }
}
