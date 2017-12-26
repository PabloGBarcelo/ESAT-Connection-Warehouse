import MySQLdb,serial,time,threading
import Adafruit_CharLCD as LCD
#!/usr/bin/env python
# -*- coding: 850 -*-
# -*- coding: UTF-8 -*-
## COM
puertoSerie = serial.Serial()
puertoSerie.port='/dev/ttyUSB0'
puertoSerie.baudrate=115200
puertoSerie.timeout=0.2

## BBDD
# Name of bbdd with all material and transport information
bbdd_sats='satsql'
# Name of bbdd of prestashop
bbdd_prestashop='BBDDTABLE'
tabla_rmas='TABLERMAS'
tabla_sats='TABLESATS-ESAT'
prefix_PS='ps_'
tabla_pedidos='TABLEORDERS'
host='HOSTBBDD'
userbbdd='USERBBDD'
bbddpass='PASSBBDD'
host2='HOSTBBDD2'
userbbdd2='USERBBDD2'
bbddpass2='PASSBBDD2'
password_read='PASS_OF_YOUR_TAGS'

# TIME LOOP
t_end = time.time() + 5 # seconds
switchAccion = 0 # 0 add from repair, 1 remove, 2 defective, 3 piece broked, 4 internal use
switchLoopButton = 0

## THREADS
threads = []
threadLimiter = threading.BoundedSemaphore(40) #Max number of threads 40max mysql
exitLoop = 0

def pintaLCD(mensaje,lcd,color=(0,0,0)):
	lcd.set_color(color[0],color[1],color[2])
	lcd.clear()
	lcd.message(mensaje)

def selectEPC(lecturaRFID): #Receive as string, not int encode to hex
	EPC=''
	# Extract 96 bits of EPC from read value
	for x in range (18,42):
		EPC=EPC+lecturaRFID[x]
	return EPC

def checkStockToUse(referencia,prefix_PS): # Check if exist stock to use in the shop
	global bbdd_prestashop
	conexion= ConectaBBDD2(bbdd_prestashop)
	cursor = conexion.cursor()
	id_product=''
	stock= ''
	''' DEPRECATED FOR PS 1.5
	thereIsStock = "SELECT quantity FROM %sproduct WHERE reference='%s'" % (prefix_PS,referencia)
	cursor.execute(thereIsStock)
	conexion.commit()
	for row in cursor:
		stock1 = row[0]
	'''
	getIdProduct = "SELECT id_product FROM %sproduct WHERE reference='%s'" % (prefix_PS,referencia)
	cursor.execute(getIdProduct)
	conexion.commit()
	for row in cursor:
		id_product=row[0]
	thereIsStock = "SELECT quantity FROM %sstock_available WHERE id_product='%s'" % (prefix_PS,id_product)
	cursor.execute(thereIsStock)
	conexion.commit()
	for row in cursor:
		stock = row[0]
	print "stock vale %s" % (stock)
	if stock>0:
		return True
	else:
		return False

def useRepairPart(puerto,numSat,lcd,threads): 
	# Open Antenna
	global t_end
	# Start with Antena 00
	OpenAntenna = bytearray.fromhex("a0 04 01 74 00 e7")
	# Read tag in Real Time
	#ParametersToReadTag2 = bytearray.fromhex("a0 04 01 89 01 d1") # Real Time
	#ParametersToReadTag2 = bytearray.fromhex("a0 04 01 80 01 da") # Buffer
	try:
		#t = threading.Thread(target=pintaLCD, args=('Leyendo TAG',lcd,)
		#threads.append(t)
		#t.start()
		puerto.open()
		puerto.write(OpenAntenna)
	except Exception, e:
		print "error open serial port: " + str(e)

	## READING t_end SECONDS
	lastEPC=''
	t_end = time.time() + 5
	while time.time() < t_end and puerto.isOpen(): # Durante t_end segundos hacemos un bucle de lectura
		global switchAccion
		if puerto.isOpen():
			# Start to read RFID
			puerto.write(bytearray.fromhex("a0 06 01 81 02 00 0c ca"))
			read_val = puerto.read(60) # 60 bytes max to read
			if len(read_val)==51:
				# Print PASAR TAG
				global tabla_pedidos
				global bbdd_sats
				global tabla_rmas
				global prefix_PS
				referencia = '' # Local variable to save reference for assign product_id
				conexion = ConectaBBDD()
				cursor = conexion.cursor()
				EPC = selectEPC(read_val.encode('hex'))
				print "switchAccion es %s" % switchAccion
				if switchAccion == 0 or switchAccion == 3: # Use to repair or Internal Use
					canBeUsed = "SELECT referencia,nSerie FROM %s WHERE EPC='%s' and uso='' and fechaSalida IS NULL and numAsociado='' and defectuosa='No' and etiquetaImprimida='Si'" % (tabla_pedidos,EPC)
					cursor.execute(canBeUsed)
					conexion.commit()
					for row in cursor:
						referencia = row[0]
						nSerie = row[1]
					# If label is available to use or sell, mark as used with numSat and Check if there is stock to use in the warehouse after check tag
					if cursor.rowcount==1 and lastEPC!=EPC and checkStockToUse(referencia,prefix_PS):
						for row in cursor:
							id_product=row[0]
						mensaje = "Num serie:\n%s" % nSerie
						#mensaje = "Tag:%s\n    %s" % (EPC[:12],EPC[-12:])
						t = threading.Thread(target=pintaLCD, args=(mensaje,lcd,(0,0,0.1)))
						threads.append(t)
						t.start()
						if switchAccion == 0:
							useToRepair= "UPDATE %s SET uso='reparacion',fechaSalida=NOW(),numAsociado='%s' WHERE EPC='%s'" % (tabla_pedidos,numSat,EPC)
							cursor.execute(useToRepair)
							conexion.commit()
						elif switchAccion == 3:
							useToInternal = "UPDATE %s SET uso='interno',fechaSalida=NOW(),numAsociado='%s' WHERE EPC='%s'" % (tabla_pedidos,numsat,EPC)
							cursor.execute(useToInternal)
							conexion.commit()
						#####################################################################################################																									#
						#      		     Aqui metemos que se actualice el stock en Prestashop y esat (total-1).   	    	#
						#####################################################################################################
						updateStockPrestashop(referencia)
						# Led or print TAG OK + DATA
					else:
						if checkStockToUse(referencia,prefix_PS):
							mensaje = 'Pieza ya gastada'
						else:
							mensaje = 'Pieza reservada\npara venta'
						t = threading.Thread(target=pintaLCD, args=(mensaje,lcd,(0.1,0,0)))
						threads.append(t)
						t.start()
						# Led or print TAG ERROR
						t_end = time.time() + 5 #Reload 5 sec to wait'''
					conexion.close()
				elif switchAccion == 1: # Enter to stock again
					canBeRestocked = "SELECT referencia FROM %s WHERE EPC='%s' and defectuosa='No' and uso!='' and etiquetaImprimida='Si' and uso!='rota'" % (tabla_pedidos,EPC)
					cursor.execute(canBeRestocked)
					conexion.commit()
					if cursor.rowcount==1 and lastEPC!=EPC:
						for row in cursor:
							referencia = row[0]
						mensaje = "Pieza repuesta"
                                                t = threading.Thread(target=pintaLCD, args=(mensaje,lcd,(0,0,0.1)))
                                                threads.append(t)
                                                t.start()
						enterToStock="UPDATE %s SET uso='', fechaSalida=NULL, numAsociado='' WHERE EPC='%s'" % (tabla_pedidos,EPC)
						cursor.execute(enterToStock)
						conexion.commit()
						updateStockPrestashop(referencia)
					else:
                                                mensaje = 'Pieza sin gastar'
                                                t = threading.Thread(target=pintaLCD, args=(mensaje,lcd,(0.1,0,0)))
                                                threads.append(t)
                                                t.start()
                                                # Led or print TAG ERROR
                                                t_end = time.time() + 5 #Reload 5 sec to wait'''
					conexion.close()
						#####################################################################################################
						#																									#
						#      		     Aqui metemos que se actualice el stock en Prestashop y esat (total-1).   			#
						#																								       #
						#####################################################################################################
				elif switchAccion == 2: # Broke during repair
					canBeTrashed = "SELECT referencia FROM %s WHERE EPC='%s' and defectuosa='No' and uso!='rota' and etiquetaImprimida='Si'" % (tabla_pedidos,EPC)
					cursor.execute(canBeTrashed)
					conexion.commit()
					if cursor.rowcount==1 and lastEPC!=EPC:
						for row in cursor:
							referencia = row[0]
						pintaLCD('Pieza a la\nbasura',lcd,(0.1,0,0))
						pieceToTrash = "UPDATE %s SET uso='rota', fechaSalida=NOW(), numAsociado='%s' WHERE EPC='%s'" % (tabla_pedidos,numSat,EPC)
						cursor.execute(pieceToTrash)
						conexion.commit()
						updateStockPrestashop(referencia)
					else:
						mensaje = 'Pieza ya marcada'
                                                t = threading.Thread(target=pintaLCD, args=(mensaje,lcd,(0.1,0,0)))
                                                threads.append(t)
                                                t.start()
					conexion.close()
				elif switchAccion == 4: # Defective part
					# Create RMA
					canBeRestocked = "SELECT referencia FROM %s WHERE EPC='%s' and defectuosa='No' and etiquetaImprimida='Si' and uso!='rota'" % (tabla_pedidos,EPC)
					cursor.execute(canBeRestocked)
					conexion.commit()
					if cursor.rowcount==1 and lastEPC!=EPC:
						for row in cursor:
							referencia = row[0]
						pintaLCD('Pieza defectuosa\nmarcada',lcd,(0.1,0,0))
						getLastID = "SELECT AUTO_INCREMENT FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '%s' AND TABLE_NAME ='%s'" % (bbdd_sats,tabla_rmas)
						cursor.execute(getLastID)
						lastID = cursor.fetchall()
						conexion.commit()
						ultimoID = ''
						for lastinserted in lastID:
							ultimoID = lastinserted[0]
						newRMA = "INSERT INTO rmaPieza (defecto,fechaAviso,fechaLlegada,fechaResolucion,aprobado,motivo) VALUES ('interno',NOW(),NOW(),NOW(),'Si','Taller-Interno')"
						cursor.execute(newRMA)
						conexion.commit()
						pieteToWarranty = "UPDATE %s SET uso='defectuosa',fechaSalida=NOW(),numAsociado='%s',defectuosa='Si',idRma=%s WHERE EPC='%s'" % (tabla_pedidos,numSat,ultimoID,EPC)
						cursor.execute(pieteToWarranty)
						conexion.commit()
						updateStockPrestashop(referencia)
					else:
						pintaLCD('Pieza no valida\npara RMA',lcd,(0.1,0,0))
					conexion.close()
				elif EPC == lastEPC:
					lcd.clear()
					lcd.message('Tag ya usada')
				t_end = time.time() + 5 #Reload 5 sec to wait
				lastEPC=EPC
		else:
			try:
				print 'hay que abrir el puerto'
				puerto.open()
			except Exception, e:
				print "error open serial port: " + str(e)
	puerto.close()

def ConectaBBDD(bbdd_sats='satsql'):
	global host, userbbdd, bbddpass
	db = MySQLdb.connect(host,userbbdd,bbddpass,bbdd_sats)
	return db

def ConectaBBDD2(bbdd_sats='drpro'):
	global host2, userbbdd2, bbddpass2
	db = MySQLdb.connect(host2,userbbdd2,bbddpass2,bbdd_sats)
	return db

def satEstadoReparado(numsat): #Check if the idSat is in state to fix or finished/canceled
	global tabla_sats
	conexion = ConectaBBDD()
	cursor = conexion.cursor()
	canBeAdded = "SELECT idSat FROM %s WHERE idSat=%s and diagnostico='' and finalizado ='No' and finSat IS NULL" % (tabla_sats,numsat)
	cursor.execute(canBeAdded)
	conexion.commit()
	conexion.close()
	if cursor.rowcount==1:
		return 1
	else:
		return 0

def cambiaLCD(lcd,buttons):
	global switchLoopButton
	global switchAccion
	global exitLoop
	lcd.set_color(0,0,0)
	while exitLoop==0:
		for button in buttons:
			if lcd.is_pressed(button[0]):
				lcd.clear()
				lcd.message(button[1])
				if button[0] == LCD.UP:
					switchAccion = 0
				elif button[0] == LCD.DOWN:
					switchAccion = 1
				elif button[0] == LCD.LEFT:
					switchAccion = 2
				elif button[0] == LCD.RIGHT:
					switchAccion = 3
				elif button[0] == LCD.SELECT:
					switchAccion = 4

#MAIN INIT
#Initializa the LCD usi2322  ng the pins
lcd = LCD.Adafruit_CharLCDPlate()
# Option buttons:
# UP - Asociar pieza (led verde) [POR DEFECTO]
# DOWN - Desasociar pieza (led naranja)
# SELECT - Marcar pieza como defectuosa (led rojo)
# LEFT - Marcar pieza como rota en proceso. (led blanco)
# RIGHT - Pieza para uso privado/interno
## Lectura del SAT
buttons = ( (LCD.UP, 'GASTAR PIEZA'),
			(LCD.DOWN, 'DEVOLVER PIEZA\n AL ALMACEN'),
			(LCD.LEFT, 'MARCAR PIEZA\nCOMO ROTA'),
			(LCD.RIGHT, 'PIEZA PARA\nUSO INTERNO'),
			(LCD.SELECT, 'PIEZA DEFECTUOSA')
			)

while 1:
	# Creamos un hilo infinito que recoja la pulsacion de los botones
	t = threading.Thread(target=cambiaLCD, args=(lcd,buttons,))
	threads.append(t)
	t.start()
	# Input sat number
	numsat=0
	exitLoop=0
	while numsat < 20000 or numsat > 1000000:
		try:
			# Inicializamos la pantalla
			pintaLCD('P SELECCIONADO:\nGASTAR PIEZA',lcd)
			switchAccion = 0
			# PRINT PASAR CODIGO DE BARRAS
			print 'Inserta Numero de SAT:'
			numsat = input("Enter a number: ")
			print numsat
		except Exception,e:
			error = str(e)
		except KeyboardInterrupt: # When press CTRL C, stop thread change screen
			exitLoop=1
			raise
	exitLoop=1
	if satEstadoReparado(numsat):
		# If exists, turn ON RFID during 10 seconds
		lcd.clear()
		lcd.message('Leyendo tag')
		switchLoopButton = 1 #Stop the catch of button
		useRepairPart(puertoSerie,numsat,lcd,threads)
		# Lanzamos un hilo con variable global por si queremos deshacer lo anadido
