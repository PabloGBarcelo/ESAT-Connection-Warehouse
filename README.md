# ESAT-Connection-Warehouse                  
 Little project for my own warehouse with:  
 3 x Raspberry PI 3 (Raspbian) with codebar reader        
 Communication with ESAT Professional       
 It use ports 
 For compile use:
 gcc -o nameFile $(mysql_config --cflags) nameFile.c $(mysql_config --libs) -lwiringPi -Wdeprecated -I/home/pi/Desktop/
  
 It use:
 PIN 3 - Buzzer for made Music when startup
 PIN 0 - Led Red
 PIN 1 - Led Green
 PIN 2 - Led Blue (status working or off)
 All comments are in Spanish (Sorry!)       
