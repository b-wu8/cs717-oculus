#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/time.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <netdb.h>
#include <sys/ipc.h>
#include <sys/shm.h>
#include <errno.h>
#include "spines_lib.h"
#include "spu_events.h"
#include "spu_alarm.h"

#define SPINES_PORT 8100
#define SEND_PORT 8400
#define RECV_PORT 8400
#define MAX_BYTES 100000

void format_ip(char* buf, int ip)
{
    unsigned char bytes[4];
    bytes[0] = ip & 0xFF;
    bytes[1] = (ip >> 8) & 0xFF;
    bytes[2] = (ip >> 16) & 0xFF;
    bytes[3] = (ip >> 24) & 0xFF;   
    sprintf(buf, "%d.%d.%d.%d", bytes[3], bytes[2], bytes[1], bytes[0]);        
}

void print_usage(void);

int main(int argc, char *argv[])
{
    int mcast_address, sk, ret;
    int i;

    char* ip;
    int local_ip;
    char temp[1024];
    char local_name[256];

    struct sockaddr *daemon_ptr = NULL;
    struct sockaddr_in remote_addr, serv_addr;
    struct hostent *h_tmp;
    struct hostent h_ent;
    char mess[1400];

    // mcast_address = (224 << 24) + 1; /* 224.0.0.1 */

    gethostname(local_name, sizeof(local_name)); //find the host name
    h_tmp = gethostbyname(local_name); //find host information
    memcpy(&local_ip, h_tmp->h_addr, sizeof(local_ip));
    local_ip = ntohl(local_ip);
    format_ip(temp, local_ip);
    printf("Local Host Name: %s (%s)\n", local_name, temp);

    serv_addr.sin_family = AF_INET;
    serv_addr.sin_port = htons(SPINES_PORT);
    memcpy(&serv_addr.sin_addr, h_tmp->h_addr, sizeof(struct in_addr));
    daemon_ptr = (struct sockaddr *)&serv_addr;
    printf("Using Spines Connection: %s@%d\n", temp, SPINES_PORT);

    if (spines_init(daemon_ptr) < 0)
    {
        printf("sender: socket error\n");
        exit(1);
    }

    if (argc > 1)
    {
        print_usage();
        return (1);
    }

    int protocol = 1; // unreliable
    sk = spines_socket(PF_SPINES, SOCK_DGRAM, protocol, daemon_ptr);
    if (ret < 0)
    {
        printf("socket error\n");
        exit(1);
    }

    remote_addr.sin_family = AF_INET;
    remote_addr.sin_addr.s_addr = INADDR_ANY;
    remote_addr.sin_port = htons(RECV_PORT);
    if (spines_bind(sk, (struct sockaddr *)&remote_addr, sizeof(remote_addr)) < 0)
    {
        perror("err: bind");
        exit(1);
    }

    memset(mess, 0, 1400);
    printf("Awaiting messages...\n");
    while (1)
    {
        mcast_address = (225 << 24) + 1;
        // sprintf(mess, "%d", address);
        ret = spines_recvfrom(sk, mess, sizeof(mess), 0, NULL, 0);
        //ret = spines_recv(sk, mess, sizeof(mess), 0);
        // ret = spines_sendto(sk, mcast_address, RECV_PORT, mess, strlen(mess) + 1);
        if (ret < 0)
        {
            printf("send error\n");
            exit(1);
        }
        printf("Received packet: \"%s\"\n", mess);
    }
}

void print_usage(void)
{
    printf("Usage: ./adv_sender\n");
}
