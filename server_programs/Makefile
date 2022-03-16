include ../Makefile.general

.PHONY: all clean distclean

top_srcdir=..

SERVER_PRORGRAMS=oculus_server adv_receiver adv_sender

all:
	pkg-config --cflags 'libprotobuf-c >= 1.0.0'
	pkg-config --libs 'libprotobuf-c >= 1.0.0'
	$(SERVER_PRORGRAMS)

oculus_server: oculus_server.o $(LIBSPINES_LIB_FILE)
		$(CC) $(LDFLAGS) -o $ oculus_server oculus_server.o $(LIBS)

oculus_server.o: oculus_server.c ./protoc_generated/game.pb-c.c
		$(CC) $(CFLAGS) -c $^

adv_receiver: adv_receiver.o $(LIBSPINES_LIB_FILE)
		$(CC) $(LDFLAGS) -o $ adv_receiver adv_receiver.o $(LIBS)

adv_sender: adv_sender.o $(LIBSPINES_LIB_FILE)
		$(CC) $(LDFLAGS) -o $ adv_sender adv_sender.o $(LIBS)

clean:
	rm -f *.o
	rm -f $(SERVER_PRORGRAMS)

distclean: clean
	rm -f *~
	rm -f core*
	rm -f Makefile