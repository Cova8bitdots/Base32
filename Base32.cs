using UnityEngine;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// RFC4648 のBase32(16進拡張じゃないほう)に基づいて作成
/// </summary>
public static class Base32
{
	/// <summary> decode用テーブル ascii code をkeyにindex の逆張りをする</summary>
	private static readonly char[] decode_table = new char[128];
	private static readonly char[] encode_table =
	{ 
		'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
		'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
		'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
		'Y', 'Z', '2', '3', '4', '5', '6', '7',
	};

	// PADDING 用文字
	static readonly char PADDING = '=';

	const int DIGIT = 5;		// Base32のbit数(binaryの桁数)
	const int BYTE = 8;			//8bit やろうなぁ
	const int BYTE_LENGTH = 8;	//40bit を5bit ずつ分割なので8個に区分け
	const int PADDING_UNIT = 4;	// Padding は4文字ごとに判定

	static bool isInitialized = false;

	/// <summary>
	/// DecodeTableの初期化
	/// </summary>
	private static void InitilizeDecodeTable()
	{
		if( isInitialized )
		{
			return;
		}
		isInitialized = true;

		for ( int i = 0, count = decode_table.Length; i < count; ++ i )
		{
			decode_table[i] = (char)0xFF;	// -1 encoding_tableにありえない値
		}

		for (int i = 0; i < encode_table.Length; i++)
		{
			decode_table[ encode_table[i] ] = (char)i;
		}
	}
	/// <summary>
	/// byte[]からbase32に変換する
	/// </summary>
	/// <param name="bytes"></param>
	/// <param name="withPadding">padding が必要かどうか</param>
	/// <returns></returns>
	public static string ToBase32String(byte[] bytes, bool withPadding = true)
	{
		System.Text.StringBuilder builder = new System.Text.StringBuilder();
		// Base32 は1文字5bitなので 5byte ずつとりだして8文字ずつに変換する
		for ( int i = 0, count = bytes.Length; i < count; i += DIGIT)
		{
			// 40bitに統合
			ulong merge = 0;
			int parseByteCount = 0;
			for (int j = 0; j < DIGIT; j++)
			{
				// 範囲外参照チェック
				if( j+i >= count )
				{
					break;
				}
				// 4-j ビットシフトが必要
				merge |= ( (ulong)bytes[i+j] << (BYTE_LENGTH * (DIGIT-j-1)) );
				parseByteCount++;
			}
			// 5bit 毎に分割
			byte b;
			for (int j = 0; j < BYTE_LENGTH; j++)
			{
				b = (byte)((merge >> DIGIT * (BYTE_LENGTH-j-1) ) & 0x1F);
				// 取り出して来た有効byteのところまで変換をかける
				if( j < parseByteCount * BYTE_LENGTH / DIGIT +1 )
				{
					builder.Append( encode_table[b]);
				}
			}
		}
		if( withPadding )
		{
			// 4文字ずつ分割して不足分は = で埋める
			int lastWordLen = builder.Length % PADDING_UNIT;
			for (int i = 0; i < PADDING_UNIT - lastWordLen; i++)
			{
				builder.Append(PADDING);
			}
		}

		return builder.ToString();
	}


	/// <summary>
	/// Base32からbyte[]に変換する
	/// </summary>
	/// <param name="base32"></param>
	/// <returns></returns>
	public static byte[] FromBase32String(string base32)
	{
		if ( 0 != (base32.Length & 3 )){
			// Base32じゃないよ！
			Debug.LogError( "not base32" );
			return null;
		}

		InitilizeDecodeTable();

		List<byte> decode = new List<byte>( base32.Length);
		// 8文字( 8 x 5bit =40bit) ずつ取り出して5byteごとに変換をかけていく
		for ( int i = 0, count = base32.Length; i < count; i += BYTE_LENGTH )
		{
			ulong merge = 0;
			for (int j = 0; j < BYTE_LENGTH; j++)
			{
				if( i + j >= count )
				{
					break;
				}
				char c = base32[i+j];
				if( c == PADDING )
				{
					break;
				}
				long index = (long)decode_table[c];
				// 改行などの対象外の場合はもう一文字
				if( index < 0 )
				{
					Debug.LogError($"Cant Found:{c}");
					continue;
				}
				// 1文字5bit なので5n bit シフトを行う
				merge |= (ulong)( index << ( (BYTE_LENGTH-j-1) * DIGIT) );

			}
			// 40bit を8bitずつに分割
			for (int j = 0; j < DIGIT; j++)
			{
				decode.Add( (byte)( (merge >> ( (DIGIT-j-1) * BYTE ) ) & 0xFF) );
			}
		}
		return decode.ToArray();
	}

}
