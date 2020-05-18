using System;
using System.Runtime.Serialization;

namespace EntityOrientedCommunication.Messages
{
    /// <summary>
    /// this message is used to transfer an object, the object will be serialized before transfering
    /// <para>when the remote computer received this message, the object will be deserilized when it is accessed</para>
    /// </summary>
    [Serializable]
    internal class EMObject<T> : EMessage
    {
        #region property
        /// <summary>
        /// the object data bytes will be converted to object of type 'T' when this property is accessed first time.
        /// </summary>
        public T Object
        {
            get
            {
                if (!bObjectRecovered)
                {
                    var obj = Configuration.Serializer.FromBytes(_objBytes);  // deserialize
                    this._object = (T)obj;
                    bObjectRecovered = true;
                }

                return _object;
            }

            set
            {
                _object = value;
                bObjectRecovered = true;
            }
        }

        #region lazy serialization, deserialization
        /// <summary>
        /// stores the serialized object
        /// </summary>
        [DataMember]
        private byte[] _objBytes;

        [NonSerialized]
        private T _object;

        /// <summary>
        /// denote the '_object' of this message has been rehabilitated from bytes to it's original form
        /// </summary>
        [NonSerialized]
        private bool bObjectRecovered;
        #endregion
        #endregion

        #region constructor
        protected EMObject()
        {

        }

        public EMObject(Envelope envelope, T @object) : base(envelope)
        {
            Object = @object;
        }

        public EMObject(EMessage toreply, T @object) : base(toreply)
        {
            Object = @object;
        }

        public EMObject(EMObject<T> copyFrom) : base(copyFrom)
        {
            _objBytes = copyFrom._objBytes;
            _object = copyFrom._object;
            this.bObjectRecovered = copyFrom.bObjectRecovered;
        }
        #endregion

        #region interface
        public override string ToString()
        {
            var objStr = Object.ToString();

            if (objStr.Length > 128)
            {
                objStr = Object.GetType().FullName;
            }

            return Format("EObj", $"{objStr}");
        }
        #endregion

        #region private
        protected override void PrepareForTransmission()
        {
            // serialize object
            if (_objBytes == null)
            {
                _objBytes = Configuration.Serializer.ToBytes(_object);
            }
        }
        #endregion
    }
}
