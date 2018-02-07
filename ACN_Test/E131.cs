//=====================================================================
//
//	E131.cs - common E1.31 classes
//
//		written from the ground up based on the protocol specifications
//		from E1.31 published documentation available. in no way is
//		this claimed to be an official implementation endorsed or
//		certified in any way.
//
//		mainly a collection of classes used to build, send, receive,
//		and manipulate the E1.31 protocol in a logical vs. physical
//		format. the three layers are represented as C# classes without
//		concerns of actual transport formatting. the PhyBuffer member
//		converts the values to/from a byte array for transport in
//		network normal byte ordering and packing.
//
//		this is not the most efficient way. it tries to be object
//		oriented instead of efficient to cleanly implement the
//		protocol.
//
//		there are other ways through interop marshalling that may
//		be more efficient for the 'conversion' between logical vs.
//		physical but they may then be less efficient for manipulation
//		of the resultant data within C# with its System.Object based
//		reference variable etc. and there is always the big-endian
//		vs. little-endian conversions to take into account.
//
//		however once built, if a copy of the phybuffer is preserved,
//		it only needs to be 'patched' in two places (sequence # and slots)
//		and reused to send another packet of the same format.
//
//		version 1.0.0.0 - 1 june 2010
//
//=====================================================================

//=====================================================================
//
// Copyright (c) 2010 Joshua 1 Systems Inc. All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are
// permitted provided that the following conditions are met:
//
//    1. Redistributions of source code must retain the above copyright notice, this list of
//       conditions and the following disclaimer.
//
//    2. Redistributions in binary form must reproduce the above copyright notice, this list
//       of conditions and the following disclaimer in the documentation and/or other materials
//       provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY JOSHUA 1 SYSTEMS INC. "AS IS" AND ANY EXPRESS OR IMPLIED
// WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
// ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
// ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
// The views and conclusions contained in the software and documentation are those of the
// authors and should not be interpreted as representing official policies, either expressed
// or implied, of Joshua 1 Systems Inc.
//
//=====================================================================

using System;
using System.Text;


namespace E131
{
	//-----------------------------------------------------------------
	//
	//	E131Base - a collection of common functions and constants
	//
	//-----------------------------------------------------------------
	public class E131Base
	{
		virtual public byte[] PhyBuffer
		{
			get
			{
				return new byte[0];
			}

			set
			{
			}
		}

		override public string ToString()
		{
			byte[]	bfr;
			string	txt = "";

			bfr = PhyBuffer;

			foreach (byte val in bfr)
			{
				txt += val.ToString("X2") + ' ';
			}

			return txt;
		}

		protected void UInt16ToBfrSwapped(UInt16 value, byte[] bfr, int offset)
		{
			bfr[offset]   = (byte) ((value & 0xff00) >> 8);
			bfr[offset+1] = (byte) (value & 0x00ff);
		}

		protected UInt16 BfrToUInt16Swapped(byte[] bfr, int offset)
		{
			return (UInt16) ((((UInt16) bfr[offset]) << 8) | ((UInt16) bfr[offset+1]));
		}

		protected void UInt32ToBfrSwapped(UInt32 value, byte[] bfr, int offset)
		{
			bfr[offset]   = (byte) ((value & 0xff000000) >> 24);
			bfr[offset+1] = (byte) ((value & 0x00ff0000) >> 16);
			bfr[offset+2] = (byte) ((value & 0x0000ff00) >>  8);
			bfr[offset+3] = (byte) ((value & 0x000000ff));
		}

		protected UInt32 BfrToUInt32Swapped(byte[] bfr, int offset)
		{
			return (UInt32) ((((UInt32) bfr[offset]) << 24) | (((UInt32) bfr[offset+1]) << 16) | (((UInt32) bfr[offset+2]) << 8) | ((UInt32) bfr[offset+3]));
		}

		protected void StringToBfr(string value, byte[] bfr, int offset, int length)
		{
			UTF8Encoding	val = new UTF8Encoding();
			byte[]			valBytes;

			valBytes = val.GetBytes(value);

			if (valBytes.Length >= length) Array.Copy(valBytes, 0, bfr, offset, length);
			else
			{
				Array.Copy(valBytes, 0, bfr, offset, valBytes.Length);

				offset += valBytes.Length;
				length -= valBytes.Length;

				while (length-- > 0) bfr[offset++] = 0;
			}
		}

		protected string BfrToString(byte[] bfr, int offset, int length)
		{
			UTF8Encoding	val = new UTF8Encoding();

			return val.GetString(bfr, offset, length);
		}

		protected void GUIDToBfr(Guid value, byte[] bfr, int offset)
		{
			byte[] valBytes = value.ToByteArray();

			Array.Copy(valBytes, 0, bfr, offset, valBytes.Length);
		}

		protected Guid BfrToGuid(byte[] bfr, int offset)
		{
			byte[]	valBytes = new byte[16];
			Guid	val;

			Array.Copy(bfr, offset, valBytes, 0, valBytes.Length);

			val = new Guid(valBytes);

			return val;
		}
	}

	
	//-----------------------------------------------------------------
	//
	//	E131Root - E1.31 Root Layer
	//
	//-----------------------------------------------------------------
	public class E131Root : E131Base
	{
		public UInt16	preambleSize;		// RLP Preamble Size (0x0010)
		public UInt16	postambleSize;		// RLP Postamble Size (0x0000)
		public string	acnPacketID;		// Identifies Packet (ASC-E1.17)
		public UInt16	flagsLength;		// PDU Flags/Length
		public UInt32	vector;				// Vector (0x00000004)
		public Guid		sendCID;			// Sender's CID

		public bool		malformed = true;	// malformed packet (length error)

		// note offsets are byte locations within the layer - not within the packet
	
		public const int	PREAMBLESIZE_OFFSET		=   0;
		public const int	POSTAMBLESIZE_OFFSET	=   2;
		public const int	ACNPACKETID_OFFSET		=   4;
		public const int	ACNPACKETID_SIZE		=  12;
		public const int	FLAGSLENGTH_OFFSET		=  16;
		public const int	VECTOR_OFFSET			=  18;
		public const int	SENDERCID_OFFSET		=  22;

		public const int	PHYBUFFER_SIZE		= 38;
		public const int	PDU_SIZE			= PHYBUFFER_SIZE - FLAGSLENGTH_OFFSET;

		public E131Root()
		{
		}

		public E131Root(UInt16 length, Guid guid)
		{
			preambleSize	= 0x0010;
			postambleSize	= 0x0000;
			acnPacketID		= "ASC-E1.17";
			flagsLength		= (UInt16) (0x7000 | length);
			vector			= 0x00000004;
			sendCID			= guid;
		}

		public E131Root(byte[] bfr, int offset)
		{
			FromBfr(bfr, offset);
		}

		public UInt16	Length
		{
			get
			{
				return (UInt16) (flagsLength & 0x0fff);
			}

			set
			{
				flagsLength = (UInt16) (0x7000 | value);
			}
		}
	
		override public byte[] PhyBuffer
		{
			get
			{
				byte[]	bfr = new byte[PHYBUFFER_SIZE];

				ToBfr(bfr, 0);

				return bfr;
			}

			set
			{
				FromBfr(value, 0);
			}
		}

		public void FromBfr(byte[] bfr, int offset)
		{
			preambleSize	= BfrToUInt16Swapped(bfr, offset + PREAMBLESIZE_OFFSET);
			postambleSize	= BfrToUInt16Swapped(bfr, offset + POSTAMBLESIZE_OFFSET);
			acnPacketID		= BfrToString(bfr, offset + ACNPACKETID_OFFSET, ACNPACKETID_SIZE);
			flagsLength		= BfrToUInt16Swapped(bfr, offset + FLAGSLENGTH_OFFSET);
			vector			= BfrToUInt32Swapped(bfr, offset + VECTOR_OFFSET);
			sendCID			= BfrToGuid(bfr, offset + SENDERCID_OFFSET);

			malformed = true;

			if (Length != bfr.Length - FLAGSLENGTH_OFFSET) return;

			malformed = false;
		}

		public void ToBfr(byte[] bfr, int offset)
		{
			UInt16ToBfrSwapped(preambleSize, bfr, offset + PREAMBLESIZE_OFFSET);
			UInt16ToBfrSwapped(postambleSize, bfr, offset + POSTAMBLESIZE_OFFSET);
			StringToBfr(acnPacketID, bfr, offset + ACNPACKETID_OFFSET, ACNPACKETID_SIZE);
			UInt16ToBfrSwapped(flagsLength, bfr, offset + FLAGSLENGTH_OFFSET);
			UInt32ToBfrSwapped(vector, bfr, offset + VECTOR_OFFSET);
			GUIDToBfr(sendCID, bfr, offset + SENDERCID_OFFSET);
		}
	}

	
	//-----------------------------------------------------------------
	//
	//	E131Framing - E1.31 Framing Layer
	//
	//-----------------------------------------------------------------
	public class E131Framing : E131Base
	{
		public UInt16	flagsLength;		// PDU Flags/Length
		public UInt32	vector;				// Vector (0x00000002)
		public string	sourceName;			// Source Name
		public byte		priority;			// Data Priority
		public UInt16	_reserved;			// reserved (0)
		public byte		sequenceNumber;		// Packet Sequence Number
		public byte		options;			// Options Flags
		public UInt16	universe;			// Universe Number

		public bool		malformed = true;	// malformed packet (length error)

		public const int	PHYBUFFER_SIZE		= 77;
		public const int	PDU_SIZE			= PHYBUFFER_SIZE;

		// note offsets are byte locations within the layer - not within the packet
	
		public const int	FLAGSLENGTH_OFFSET		=   0;
		public const int	VECTOR_OFFSET			=   2;
		public const int	SOURCENAME_OFFSET		=   6;
		public const int	SOURCENAME_SIZE			=  64;
		public const int	PRIORITY_OFFSET			=  70;
		public const int	_RESERVED_OFFSET		=  71;
		public const int	SEQUENCENUMBER_OFFSET	=  73;
		public const int	OPTIONS_OFFSET			=  74;
		public const int	UNIVERSE_OFFSET			=  75;

		public E131Framing()
		{
		}

		public E131Framing(UInt16 length, string source, byte sequence, UInt16 univ)
		{
			flagsLength		= (UInt16) (0x7000 | length);
			vector			= 0x00000002;
			sourceName		= source;
			priority		= 100;
			_reserved		= 0;
			sequenceNumber	= sequence;
			options			= 0;
			universe		= univ;
		}

		public E131Framing(byte[] bfr, int offset)
		{
			FromBfr(bfr, offset);
		}

		public UInt16	Length
		{
			get
			{
				return (UInt16) (flagsLength & 0x0fff);
			}

			set
			{
				flagsLength = (UInt16) (0x7000 | value);
			}
		}
	
		override public byte[] PhyBuffer
		{
			get
			{
				byte[]	bfr = new byte[PHYBUFFER_SIZE];

				ToBfr(bfr, 0);

				return bfr;
			}

			set
			{
				FromBfr(value, 0);
			}
		}

		public void FromBfr(byte[] bfr, int offset)
		{
			flagsLength		= BfrToUInt16Swapped(bfr, offset + FLAGSLENGTH_OFFSET);
			vector			= BfrToUInt32Swapped(bfr, offset + VECTOR_OFFSET);
			sourceName		= BfrToString(bfr, offset + SOURCENAME_OFFSET, SOURCENAME_SIZE);
			priority		= bfr[offset + PRIORITY_OFFSET];
			_reserved		= BfrToUInt16Swapped(bfr, offset + _RESERVED_OFFSET);
			sequenceNumber	= bfr[offset + SEQUENCENUMBER_OFFSET];
			options			= bfr[offset + OPTIONS_OFFSET];
			universe		= BfrToUInt16Swapped(bfr, offset + UNIVERSE_OFFSET);

			malformed = true;

			if (Length != bfr.Length - E131Root.PHYBUFFER_SIZE) return;

			malformed = false;
		}

		public void ToBfr(byte[] bfr, int offset)
		{
			UInt16ToBfrSwapped(flagsLength, bfr, offset + FLAGSLENGTH_OFFSET);
			UInt32ToBfrSwapped(vector, bfr, offset + VECTOR_OFFSET);
			StringToBfr(sourceName, bfr, offset + SOURCENAME_OFFSET, SOURCENAME_SIZE);
			bfr[offset + PRIORITY_OFFSET] = priority;
			UInt16ToBfrSwapped(_reserved, bfr, offset + _RESERVED_OFFSET);
			bfr[offset + SEQUENCENUMBER_OFFSET] = sequenceNumber;
			bfr[offset + OPTIONS_OFFSET] = options;
			UInt16ToBfrSwapped(universe, bfr, offset + UNIVERSE_OFFSET);
		}
	}
	

	//-----------------------------------------------------------------
	//
	//	E131DMP - E1.31 DMP Layer
	//
	//-----------------------------------------------------------------
	public class E131DMP : E131Base
	{
		public UInt16	flagsLength;		// DMP PDU Flags/Length
		public byte		vector;				// DMP Vector (0x02)
		public byte		addrTypeDataType;	// Address Type / Data Type (0xa1)
		public UInt16	firstPropertyAddr;	// DMX Start At DMP 0 (0x0000)
		public UInt16	addrIncrement;		// Property Size (0x0001)
		public UInt16	propertyValueCnt;	// Property Value Count
		public byte[]	propertyValues;		// Property Values

		public bool		malformed = true;	// malformed packet (length error)

		public const int	PHYBUFFER_BASE		= 10;
		public const int	PDU_BASE			= PHYBUFFER_BASE;

		public const int	FLAGSLENGTH_OFFSET			=   0;
		public const int	VECTOR_OFFSET				=   2;
		public const int	ADDRTYPEDATATYPE_OFFSET		=   3;
		public const int	FIRSTPROPERTYADDR_OFFSET	=   4;
		public const int	ADDRINCREMENT_OFFSET		=   6;
		public const int	PROPERTYVALUECNT_OFFSET		=   8;
		public const int	PROPERTYVALUES_OFFSET		=  10;

		public E131DMP()
		{
		}

		public E131DMP(byte[] values, int offset, int slots)
		{
			flagsLength			= (UInt16) (0x7000 | (PDU_BASE + 1 + slots));
			vector				= 0x02;
			addrTypeDataType	= 0xa1;
			firstPropertyAddr	= 0x0000;
			addrIncrement		= 0x0001;
			propertyValueCnt	= (UInt16) (slots + 1);
			propertyValues		= new byte[slots + 1];
			propertyValues[0]   = 0;
			Array.Copy(values, offset, propertyValues, 1, slots);
		}

		public E131DMP(byte[] bfr, int offset)
		{
			FromBfr(bfr, offset);
		}

		public UInt16 PhyLength
		{
			get
			{
				return (UInt16) (PHYBUFFER_BASE + propertyValueCnt);
			}
		}

		public UInt16	Length
		{
			get
			{
				return (UInt16) (flagsLength & 0x0fff);
			}

			set
			{
				flagsLength = (UInt16) (0x7000 | value);
			}
		}
	
		override public byte[] PhyBuffer
		{
			get
			{
				byte[]	bfr = new byte[PhyLength];

				ToBfr(bfr, 0);

				return bfr;
			}

			set
			{
				FromBfr(value, 0);
			}
		}

		public void FromBfr(byte[] bfr, int offset)
		{
			flagsLength			= BfrToUInt16Swapped(bfr, offset + FLAGSLENGTH_OFFSET);
			vector				= bfr[offset + VECTOR_OFFSET];
			addrTypeDataType	= bfr[offset + ADDRTYPEDATATYPE_OFFSET];
			firstPropertyAddr	= BfrToUInt16Swapped(bfr, offset + FIRSTPROPERTYADDR_OFFSET);
			addrIncrement		= BfrToUInt16Swapped(bfr, offset + ADDRINCREMENT_OFFSET);
			propertyValueCnt	= BfrToUInt16Swapped(bfr, offset + PROPERTYVALUECNT_OFFSET);
			propertyValues		= new byte[propertyValueCnt];

			malformed = true;

			Array.Copy(bfr, offset + PROPERTYVALUES_OFFSET, propertyValues, 0, propertyValueCnt);

			malformed = false;
		}

		public void ToBfr(byte[] bfr, int offset)
		{
			UInt16ToBfrSwapped(flagsLength, bfr, offset + FLAGSLENGTH_OFFSET);
			bfr[offset + VECTOR_OFFSET] = vector;
			bfr[offset + ADDRTYPEDATATYPE_OFFSET] = addrTypeDataType;
			UInt16ToBfrSwapped(firstPropertyAddr, bfr, offset + FIRSTPROPERTYADDR_OFFSET);
			UInt16ToBfrSwapped(addrIncrement, bfr, offset + ADDRINCREMENT_OFFSET);
			UInt16ToBfrSwapped(propertyValueCnt, bfr, offset + PROPERTYVALUECNT_OFFSET);
			Array.Copy(propertyValues, 0, bfr, offset + PROPERTYVALUES_OFFSET, propertyValueCnt);
		}
	}

	
	//-----------------------------------------------------------------
	//
	//	E131Pkt - E1.31 The Packet
	//
	//-----------------------------------------------------------------
	public class E131Pkt : E131Base
	{
		public E131Root		e131Root;			// root layer
		public E131Framing	e131Framing;		// framing layer
		public E131DMP		e131DMP;			// dmp layer

		public bool			malformed = true;	// malformed packet (length error)

		const int	ROOT_OFFSET			=   0;
		const int	FRAMING_OFFSET		= E131Root.PHYBUFFER_SIZE;
		const int	DMP_OFFSET			= E131Root.PHYBUFFER_SIZE + E131Framing.PHYBUFFER_SIZE;

		public E131Pkt()
		{
		}

		public E131Pkt(Guid guid, string source, byte sequence, UInt16 universe, byte[] values, int offset, int slots)
		{
			e131DMP		= new E131DMP(values, offset, slots);
			e131Framing	= new E131Framing((UInt16) (E131Framing.PHYBUFFER_SIZE + e131DMP.Length), source, sequence, universe);
			e131Root	= new E131Root((UInt16) (E131Root.PDU_SIZE + e131Framing.Length), guid);
		}

		public E131Pkt(byte[] bfr)
		{
			PhyBuffer = bfr;
		}

		public UInt16 PhyLength
		{
			get
			{
				return (UInt16) (E131Root.PHYBUFFER_SIZE + e131Framing.Length);
			}
		}

		override public byte[] PhyBuffer
		{
			get
			{
				byte[]	bfr = new byte[PhyLength];

				e131Root.ToBfr(bfr, ROOT_OFFSET);
				e131Framing.ToBfr(bfr, FRAMING_OFFSET);
				e131DMP.ToBfr(bfr, DMP_OFFSET);

				return bfr;
			}

			set
			{
				malformed = true;		// assume malformed

				if (value.Length < E131Root.PHYBUFFER_SIZE + E131Framing.PHYBUFFER_SIZE + E131DMP.PHYBUFFER_BASE) return;

				e131Root = new E131Root(value, ROOT_OFFSET);
				if (e131Root.malformed) return;

				e131Framing = new E131Framing(value, FRAMING_OFFSET);
				if (e131Root.malformed) return;

				e131DMP = new E131DMP(value, DMP_OFFSET);
				if (e131DMP.malformed) return;

				malformed = false;
			}
		}

		
		//-------------------------------------------------------------
		//
		//	CompareSlots() - compare a new event buffer against current
		//					 slots
		//
		//		this is a static function to work on prebuilt packets.
		//		it is embedded in the E131Pkt class to keep it with
		//		the constants and rules that were used to build the
		//		original packet.
		//
		//-------------------------------------------------------------
		static public bool CompareSlots(byte[] phyBuffer, byte[] values, int offset, int slots)
		{
			int	idx = E131Root.PHYBUFFER_SIZE + E131Framing.PHYBUFFER_SIZE + E131DMP.PROPERTYVALUES_OFFSET + 1;

			while (slots-- > 0)
			{
				if (phyBuffer[idx++] != values[offset++]) return false;
			}

			return true;
		}

		
		//-------------------------------------------------------------
		//
		//	CopySlotsSeqNum() - copy a new sequence # and slots into
		//						an existing packet buffer
		//
		//		this is a static function to work on prebuilt packets.
		//		it is embedded in the E131Pkt class to keep it with
		//		the constants and rules that were used to build the
		//		original packet.
		//
		//-------------------------------------------------------------
		static public void CopySeqNumSlots(byte[] phyBuffer, byte[] values, int offset, int slots, byte seqNum)
		{
			int	idx = E131Root.PHYBUFFER_SIZE + E131Framing.PHYBUFFER_SIZE + E131DMP.PROPERTYVALUES_OFFSET + 1;

			Array.Copy(values, offset, phyBuffer, idx, slots);

			phyBuffer[E131Root.PHYBUFFER_SIZE + E131Framing.SEQUENCENUMBER_OFFSET] = seqNum;
		}
	}
}
