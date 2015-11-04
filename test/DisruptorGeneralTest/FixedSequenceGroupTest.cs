using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;
using Disruptor;
namespace DisruptorGeneralTest
{
    [TestFixture]
    public class FixedSequenceGroupTest
    {
        [Test]
        public void ShouldReturnMinimumOf2Sequences()
        {
            var sequence1 = new Sequence(34);
            var sequnece2 = new Sequence(47);
            var group = new FixedSequenceGroup(new Sequence[] { sequence1, sequnece2 });

            Assert.AreEqual(group.Value, 34);
            sequence1.Value = 35;
            Assert.AreEqual(group.Value, 35);
            sequence1.Value = 48;
            Assert.AreEqual(group.Value, 47);
        }
    }
}
