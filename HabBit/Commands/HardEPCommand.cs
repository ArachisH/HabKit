using System;
using System.Collections.Generic;

namespace HabBit.Commands
{
    public class HardEPCommand : Command
    {
        public Uri Address { get; set; }

        public override void Populate(Queue<string> parameters)
        {
            Address = new Uri("http://" + parameters.Dequeue());
        }
    }
}