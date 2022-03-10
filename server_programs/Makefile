include ../Makefile.general

.PHONY: all clean distclean

top_srcdir=..

SERVER_PRORGRAMS=adv_pong_pos adv_pong adv_receiver adv_sender

all: $(SERVER_PRORGRAMS)

adv_pong_pos: adv_pong_pos.o $(LIBSPINES_LIB_FILE)
		$(CC) $(LDFLAGS) -o $ adv_pong_pos adv_pong_pos.o $(LIBS)

adv_pong: adv_pong.o $(LIBSPINES_LIB_FILE)
		$(CC) $(LDFLAGS) -o $ adv_pong adv_pong.o $(LIBS)

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