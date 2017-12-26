import MySQLdb
import serial
import time
import threading
import Adafruit_CharLCD as LCD
#!/usr/bin/env python
# -*- coding: 850 -*-
# -*- coding: UTF-8 -*-
## COM
puertoSerie = serial.Serial()
puertoSerie.port='/dev/ttyUSB0'
puertoSerie.baudrate=115200
puertoSerie.timeout=0.5

## BBDD
bbdd_sats='MYSQLNAME'
tabla_rmas='RMASQLNAME'
tabla_sats='SATSQLNAME'
tabla_pedidos='SATSQLORDERNAME'
host='HOSTNAMEMYSQL'
userbbdd='BBDDMYSQL'
bbddpass='PASSWORDPASS'
password_read='TAG PASSWORD FOR READ'

# TIME LOOP
t_end = 5 # seconds
switchAccion = 0 # 0 add from repair, 1 remove, 2 defective, 3 piece broked, 4 internal use
switchLoopButton = 0

## THREADS
threads = []
threadLimiter = threading.BoundedSemaphore(40) #Max number of threads 45max mysql


def selectEPC(lecturaRFID): #Receive as string, not int encode to hex
	EPC=''
	# Extract 96 bits of EPC from read value
	for x in range (18,42):
		EPC=EPC+lecturaRFID[x]
	return EPC

def useRepairPart(puerto,numSat,lcd): 
	# Open Antenna
	global t_end
	OpenAntenna = bytearray.fromhex("a0 04 01 74 00 e7")
	# Read tag in Real Time
	#ParametersToReadTag2 = bytearray.fromhex("a0 04 01 89 01 d1") # Real Time
	#ParametersToReadTag2 = bytearray.fromhex("a0 04 01 80 01 da") # Buffer
	try:
		puerto.open()
		puerto.write(OpenAntenna)
		lcd.clear()
		lcd.message('Leyendo Tags...')
	except Exception, e:
		print "error open serial port: " + str(e)

	## READING t_end SECONDS
	lastEPC=''
	t_end = time.time() + t_end
	while time.time() < t_end and puerto.isOpen(): # After t_end seconds we made a loop for read again
		global switchAccion
		if puerto.isOpen():
			puerto.write(bytearray.fromhex("a0 06 01 81 02 00 0c ca"))
			read_val = puerto.read(60) # 60 bytes max to read
			if len(read_val)==51:
				# Print READ TAG
				global tabla_pedidos
				global bbdd_sats
				global tabla_rmas
				conexion = ConectaBBDD()
				cursor = conexion.cursor()
				EPC = selectEPC(read_val.encode('hex'))
				if switchAccion == 0 or switchAccion == 3 and EPC!=lastEPC: # Use to repair
					if switchAccion == 0:
						canBeUsed = "SELECT EPC FROM %s WHERE EPC='%s' and uso='' and fechaSalida IS NULL and numAsociado='' and defectuosa='No' and etiquetaImprimida='Si'" % (tabla_pedidos,EPC)
						cursor.execute(canBeUsed)
						conexion.commit()
					elif switchAccion == 4:
						internalUse = "SELECT EPC FROM %s WHERE EPC='%s' and uso='' and fechaSalida IS NULL and numAsociado='' and defectuosa='No' and etiquetaImprimida='Si'" % (tabla_pedidos,EPC)
						cursor.execute(internalUse)
						conexion.commit()
					# If label is available to use or sell, mark as used with numSat
					if cursor.rowcount==1 and lastEPC!=EPC:
						lcd.clear()
						lcd.message('Tag encontrada\n%s') % EPC
						useToRepair= "UPDATE %s SET uso='reparacion',fechaSalida=NOW(),numAsociado='%s' WHERE EPC='%s'" % (tabla_pedidos,numSat,EPC)
						cursor.execute(useToRepair)
						conexion.commit()
						#####################################################################################################
						#																									#
						#      		                UPDATE STOCK Prestashop and esat (total-1).   	                		#
						#																									#
						#####################################################################################################
						# Led or print TAG OK + DATA
					else:
						print 'No se puede usar'
						# Led or print TAG ERROR
						t_end = time.time() + 5 #Reload 5 sec to wait'''
					conexion.close()
				elif switchAccion == 1 and EPC!=lastEPC: # Enter to stock again
					canBeRestocked = "SELECT EPC FROM %s WHERE EPC='%s' and defectuosa='No' and uso!='' and etiquetaImprimida=='Si' and uso!='rota'" % (tabla_pedidos,EPC)
					cursor.execute(canBeRestocked)
					conexion.commit()
					if cursor.rowcount==1 and lastEPC!=EPC:
						print 'Se puede reponer'
						lcd.clear()
						lcd.message('Tag encontrada\n%s') % EPC
						enterToStock="UPDATE %s SET uso='', fechaSalida='', numAsociado='' WHERE EPC='%s'" % (tabla_pedidos,EPC)
						cursor.execute(enterToStock)
						conexion.commit()
						#####################################################################################################
						#																									#
						#      		     Aqui metemos que se actualice el stock en Prestashop y esat (total-1).   			#
						#																									#
						#####################################################################################################
				elif switchAccion == 2 and EPC!=lastEPC: # Broke during repair
					lcd.clear()
					lcd.message('Pieza a la\nbasura')
					pieceToTrash = "UPDATE %s SET uso='rota', fechaSalida=NOW(), numAsociado='%s' WHERE EPC='%s')" % (tabla_pedidos,numSat,EPC)
					cursor.execute(pieceToTrash)
					conexion.commit()
				elif switchAccion == 4 and EPC!=lastEPC: # Defective part
					# Create RMA
					canBeRestocked = "SELECT EPC FROM %s WHERE EPC='%s' and defectuosa='No' and uso!='' and etiquetaImprimida=='Si' and uso!='rota'" % (tabla_pedidos,EPC)
					cursor.execute(canBeRestocked)
					conexion.commit()
					if cursor.rowcount==1 and lastEPC!=EPC:
						lcd.clear()
						lcd.message('Pieza defectuosa\n marcada')
						getLastID = "SELECT AUTO_INCREMENT FROM  INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = %s AND TABLE_NAME = %s'TableName'" % (bbdd_sats,tabla_rmas)
						cursor.execute(getLastID)
						lastID = cursor.fetchall()
						conexion.commit()
						ultimoID = ''
						for lastinserted in lastID:
							ultimoID = lastinserted[0]
						newRMA = "INSERT INTO rmaPieza (fechaResolucion,aprobado,motivo) VALUES (NOW(),'Si','Taller-Interno') WHERE EPC='%s'" % EPC
						cursor.execute(newRMA)
						conexion.commit()
						pieteToWarranty = "UPDATE %s SET uso='defectuosa', fechaSalida=NOW(), numAsociado='%s', defectuosa='Si',  idRma = %s WHERE EPC='%s'" % (tabla_pedidos,numSat,EPC,ultimoID)
						cursor.execute(pieteToWarranty)
						conexion.commit()
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

def ConectaBBDD():
	global host, bbdd_sats, userbbdd, bbddpass
	db = MySQLdb.connect(host,userbbdd,bbddpass,bbdd_sats)
	return db

def satEstadoReparado(numsat):
	global tabla_sats
	conexion = ConectaBBDD()
	cursor = conexion.cursor()
	canBeAdded = "SELECT idSat FROM %s WHERE idSat=%s and diagnostico='' and finalizado ='No' and finSat IS NULL" % (tabla_sats,numsat)
	print canBeAdded
	cursor.execute(canBeAdded)
	conexion.commit()
	conexion.close()
	if cursor.rowcount==1:
		return 1
	else:
		return 0

def cambiaLCD(lcd,buttons):
	global switchLoopButton
	while 1:
		if switchLoopButton == 0:	
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

# Creamos un hilo infinito que recoja la pulsacion de los botones
t = threading.Thread(target=cambiaLCD, args=(lcd,buttons,))
threads.append(t)
t.start()
while 1:
	# Input sat number
	numsat=0
	while numsat < 20000 or numsat > 100000 or switchAccion==4:
		try:
			# OPEN COMMAND TO USE BUTTONS
			switchLoopButton = 0 #start the catch of button
			# Inicializamos la pantalla
			lcd.clear()
			lcd.message('GASTAR PIEZA')
			switchAccion = 0
			# PRINT PASAR CODIGO DE BARRAS
			print 'Inserta Numero de SAT:'
			numsat = input("Enter a number: ")
			print numsat
		except Exception,e:
			error = str(e)
	# If exists, turn ON RFID during 10 seconds
	if switchAccion !=4:
		if satEstadoReparado(numsat):
			switchLoopButton = 1 #Stop the catch of button
			useRepairPart(puertoSerie,numsat,lcd)
	elif switchAccion ==4:
		if satEstadoReparado(0):
			switchLoopButton = 1 #Stop the catch of button
			useRepairPart(puertoSerie,numsat,lcd)

		# Lanzamos un hilo con variable global por si queremos deshacer lo anadido