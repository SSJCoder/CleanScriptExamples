/*

usage example:

	** ENCODING BINARY **

buf := new std.buffer ()
buf.add( std.DATA_FL32, 1/3 )
buf.add( std.DATA_UI8, 0 )
buf.add( std.DATA_UI8, 1 )
buf.add( std.DATA_UI8, 2 )
buf.add( std.DATA_UI8, 3 )
buf.add( std.DATA_STRZ, "Hello World" )

bin := buf.bin()

	** DECODING BINARY **

buf = new std.buffer (bin.$buffer)
prt "read begin;;"
prt buf.read_fl32()
prt buf.read_ui8()
prt buf.read_ui8()
prt buf.read_ui8()
prt buf.read_ui8()
prt buf.read_strz()
prt "read end;;"

*/

std.{
	// data kinds
	enum dataKinds {
		DATA_UI8, 
		DATA_UI16, 
		DATA_UI32, 
		DATA_I8, 
		DATA_I16, 
		DATA_I32, 
		DATA_FL32, 
		DATA_FL64, 
		DATA_STR, 
		DATA_STRZ
	}
}

class std.buffer {
	list => new [:item]
	size => 0
	
	// read position
	read_pos => 0
	
	// amount of unread bytes left
	unreadBytes => 0
	get unreadBytes () -> num {
		ret max( 0, arrayBuffer.byteLength:$num-read_pos )
	}
	
	//..
	   dataView:unc
	arrayBuffer:unc
	
	class item {
		kind => -1
		lite => true // little-endian mode
		// item
		item:unc
		
		constructor (kind:num, item:unc, lite:bol|true) {
			// set properties
			self.kind = kind
			self.item = item
			self.lite = lite
		}
	}
	
	constructor () {
		//..
	}
	
	constructor (str:str) {
		// create buffer from string
		arrayBuffer = str->bin().$buffer
		
		// create data view
		dataView = new $DataView (arrayBuffer)
	}
	
	constructor (buf:unc) {
		if ( buf != NULL ) {
			// create data view
			dataView = new $DataView (buf)
			
			// set array buffer
			arrayBuffer = buf
		}
	}
}

std.buffer.{
	read_ui8 () -> num {
		value := dataView.getUint8( read_pos ):$num, read_pos++
		ret value
	}
	
	read_ui16 (lite:bol|true) -> num {
		value := dataView.getUint16( read_pos, lite ):$num, read_pos+=2
		ret value
	}
	
	read_ui32 (lite:bol|true) -> num {
		value := dataView.getUint32( read_pos, lite ):$num, read_pos+=4
		ret value
	}
	
	read_i8 () -> num {
		value := dataView.getInt8( read_pos ):$num, read_pos++
		ret value
	}
	
	read_i16 (lite:bol|true) -> num {
		value := dataView.getInt16( read_pos, lite ):$num, read_pos+=2
		ret value
	}
	
	read_i32 (lite:bol|true) -> num {
		value := dataView.getInt32( read_pos, lite ):$num, read_pos+=4
		ret value
	}
	
	read_fl32 (lite:bol|true) -> num {
		value := dataView.getFloat32( read_pos, lite ):$num, read_pos+=4
		ret value
	}
	
	read_fl64 (lite:bol|true) -> num {
		value := dataView.getFloat64( read_pos, lite ):$num, read_pos+=8
		ret value
	}
	
	read_str (size:num) -> str {
		// ascii character lookup array
		asc_chr := internal::asc_chr
		
		str := ""
		  i := read_pos
		  e := read_pos+size
		loop (i<e), i++ {
			str += asc_chr[ dataView.getUint8( i ):$num ]
		}
		
		// update read position
		read_pos = i
		
		// give string
		ret str
	}
	
	read_strz () -> str {
		// ascii character lookup array
		asc_chr := internal::asc_chr
		
		// read string
		str := ""
		  i := read_pos
		  e := arrayBuffer.byteLength:$num
		loop (i<e), i++ {
			asc := dataView.getUint8( i ):$num
			if ( asc != 0 ) {
				str += asc_chr[asc]
			} else {
				i++, stop
			}
		}
		
		// update read position
		read_pos = i
		
		// give string
		ret str
	}
	
	add (kind:num, value:num, lite:bol|true) -> void {
		// list item
		list->push( new item (kind, value, lite) )
		
		// update size
		case kind {
			std.DATA_UI8, 
			std.DATA_I8 => size++
			
			std.DATA_UI16, 
			std.DATA_I16 => size += 2
			
			std.DATA_UI32, 
			std.DATA_I32, 
			std.DATA_FL32 => size += 4
			
			std.DATA_FL64 => size += 8
		}
	}
	
	add (kind:num, value:str) -> void {
		// list item
		list->push( new item (kind, value) )
		
		// update size
		size += value->length + ((kind == std.DATA_STRZ) ? (1,0))
	}
	
	bin () -> [:num] {
		// create data view
		view:unc = new $DataView (new $ArrayBuffer (size))
		// iterate
		i := 0
		itr list (cont~) {
			case cont.kind {
				// unsigned integers
				std.DATA_UI8 => {
					view.setUint8 ( i, cont.item:$num ), i++
				}
				
				std.DATA_UI16 => {
					view.setUint16( i, cont.item:$num, cont.lite ), i+=2
				}
				
				std.DATA_UI32 => {
					view.setUint32( i, cont.item:$num, cont.lite ), i+=4
				}
				
				// signed integers
				std.DATA_I8 => {
					view.setInt8  ( i, cont.item:$num ), i++
				}
				
				std.DATA_I16 => {
					view.setInt16 ( i, cont.item:$num, cont.lite ), i+=2
				}
				
				std.DATA_I32 => {
					view.setInt32 ( i, cont.item:$num, cont.lite ), i+=4
				}
				
				// floats
				std.DATA_FL32 => {
					view.setFloat32( i, cont.item:$num, cont.lite ), i+=4
				}
				
				std.DATA_FL64 => {
					view.setFloat64( i, cont.item:$num, cont.lite ), i+=8
				}
				
				// string
				std.DATA_STR => {
					str := cont.item:$str
					 si := 0
					 se := str->length
					loop (si<se), i,si++ {
						view.setUint8( i, str->asc( si ) )
					}
				}
				
				// zero-terminated string
				std.DATA_STRZ => {
					str := cont.item:$str
					 si := 0
					 se := str->length
					loop (si<se), i,si++ {
						view.setUint8( i, str->asc( si ) )
					}
					
					// terminate
					view.setUint8( i, 0 ), i++
				}
			}
		}
		
		// give array
		ret (new $Uint8Array (view.buffer)):$[:num]
	}
	
	str () -> str {
		ret bin()->ascstr()
	}
}

