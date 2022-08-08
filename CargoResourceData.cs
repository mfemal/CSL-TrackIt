using System.Text;
using ColossalFramework.IO;

namespace TrackIt.API
{
    public struct CargoResourceData
    {
        public uint averageAgriculture;
        public uint averageFish;
        public uint averageForestry;
        public uint averageGoods;
        public uint averageMail;
        public uint averageOil;
        public uint averageOre;

        internal uint _tempAgriculture;
        internal uint _tempFish;
        internal uint _tempForestry;
        internal uint _tempGoods;
        internal uint _tempMail;
        internal uint _tempOil;
        internal uint _tempOre;

        private uint _finalAgriculture;
        private uint _finalFish;
        private uint _finalForestry;
        private uint _finalGoods;
        private uint _finalMail;
        private uint _finalOil;
        private uint _finalOre;

        public void Add(ref CargoResourceData data)
        {
            _tempAgriculture += data._tempAgriculture;
            _tempFish += data._tempFish;
            _tempForestry += data._tempForestry;
            _tempGoods += data._tempGoods;
            _tempMail += data._tempMail;
            _tempOil += data._tempOil;
            _tempOre += data._tempOre;
        }

        public static CargoResourceData operator +(CargoResourceData input1, CargoResourceData input2)
        {
            return new CargoResourceData
            {
                averageAgriculture = input1.averageAgriculture + input2.averageAgriculture,
                averageFish = input1.averageFish + input2.averageFish,
                averageForestry = input1.averageForestry + input2.averageForestry,
                averageGoods = input1.averageGoods + input2.averageGoods,
                averageMail = input1.averageMail + input2.averageMail,
                averageOil = input1.averageOil + input2.averageOil,
                averageOre = input1.averageOre + input2.averageOre,

                _finalAgriculture = input1._finalAgriculture + input2._finalAgriculture,
                _finalFish = input1._finalFish + input2._finalFish,
                _finalForestry = input1._finalForestry + input2._finalForestry,
                _finalGoods = input1._finalGoods + input2._finalGoods,
                _finalMail = input1.averageMail + input2.averageMail,
                _finalOil = input1._finalOil + input2._finalOil,
                _finalOre = input1._finalOre + input2._finalOre,

                _tempAgriculture = input1._tempAgriculture + input2._tempAgriculture,
                _tempFish = input1._tempFish + input2._tempFish,
                _tempForestry = input1._tempForestry + input2._tempForestry,
                _tempGoods = input1._tempGoods + input2._tempGoods,
                _tempMail = input1._tempMail + input2._tempMail,
                _tempOil = input1._tempOil + input2._tempOil,
                _tempOre = input1._tempOre + input2._tempOre
            };
        }

        public long Total()
        {
            return averageAgriculture + averageFish + averageForestry + averageGoods + averageMail + averageOil + averageOre;
        }

        public override string ToString()
        {
            return new StringBuilder()
                .AppendFormat("Agriculture {0}", averageAgriculture)
                .AppendFormat(", Fish {0}", averageFish)
                .AppendFormat(", Forestry {0}", averageForestry)
                .AppendFormat(", Goods {0}", averageGoods)
                .AppendFormat(", Mail {0}", averageMail)
                .AppendFormat(", Oil {0}", averageOil)
                .AppendFormat(", Ore {0}", averageOre)
                .ToString();
        }

        public void Update()
        {
            averageAgriculture = (averageAgriculture * 18 + _finalAgriculture + _tempAgriculture + 18) / 20u;
            averageFish = (averageFish * 18 + _finalFish + _finalFish + 18) / 20u;
            averageForestry = (averageForestry * 18 + _finalForestry + _tempForestry + 18) / 20u;
            averageGoods = (averageGoods * 18 + _finalGoods + _tempGoods + 18) / 20u;
            averageMail = (averageMail * 18 + _finalMail + _finalMail + 18) / 20u;
            averageOil = (averageOil * 18 + _finalOil + _tempOil + 18) / 20u;
            averageOre = (averageOre * 18 + _finalOre + _tempOre + 18) / 20u;

            _finalAgriculture = _tempAgriculture;
            _finalFish = _tempFish;
            _finalForestry = _tempForestry;
            _finalGoods = _tempGoods;
            _finalMail = _tempMail;
            _finalOil = _tempOil;
            _finalOre = _tempOre;

            Reset();
        }

        public void Reset()
        {
            _tempAgriculture = 0u;
            _tempFish = 0u;
            _tempForestry = 0u;
            _tempGoods = 0u;
            _tempMail = 0u;
            _tempOil = 0u;
            _tempOre = 0u;
        }

        public void Serialize(DataSerializer s)
        {
            s.WriteUInt32(_tempAgriculture);
            s.WriteUInt32(_tempFish);
            s.WriteUInt32(_tempForestry);
            s.WriteUInt32(_tempGoods);
            s.WriteUInt32(_tempMail);
            s.WriteUInt32(_tempOil);
            s.WriteUInt32(_tempOre);

            s.WriteUInt32(_finalAgriculture);
            s.WriteUInt32(_finalFish);
            s.WriteUInt32(_finalForestry);
            s.WriteUInt32(_finalGoods);
            s.WriteUInt32(_finalMail);
            s.WriteUInt32(_finalOil);
            s.WriteUInt32(_finalOre);

            s.WriteUInt32(averageAgriculture);
            s.WriteUInt32(averageFish);
            s.WriteUInt32(averageForestry);
            s.WriteUInt32(averageGoods);
            s.WriteUInt32(averageMail);
            s.WriteUInt32(averageOil);
            s.WriteUInt32(averageOre);
        }

        public void Deserialize(DataSerializer s)
        {
            _tempAgriculture = s.ReadUInt32();
            _tempFish = s.ReadUInt32();
            _tempForestry = s.ReadUInt32();
            _tempGoods = s.ReadUInt32();
            _tempMail = s.ReadUInt32();
            _tempOil = s.ReadUInt32();
            _tempOre = s.ReadUInt32();

            _finalAgriculture = s.ReadUInt32();
            _finalFish = s.ReadUInt32();
            _finalForestry = s.ReadUInt32();
            _finalGoods = s.ReadUInt32();
            _finalMail = s.ReadUInt32();
            _finalOil = s.ReadUInt32();
            _finalOre = s.ReadUInt32();

            averageAgriculture = s.ReadUInt32();
            averageFish = s.ReadUInt32();
            averageForestry = s.ReadUInt32();
            averageGoods = s.ReadUInt32();
            averageMail = s.ReadUInt32();
            averageOil = s.ReadUInt32();
            averageOre = s.ReadUInt32();
        }
    }
}
