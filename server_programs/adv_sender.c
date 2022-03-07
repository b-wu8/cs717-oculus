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

void format_ip(char *buf, int ip)
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

    char *ip;
    int local_ip, remote_ip;
    char temp[1024];
    char local_name[256];
    char remote_name[256];

    struct sockaddr *daemon_ptr = NULL;
    struct sockaddr_in name, host, serv_addr;
    struct hostent *h_tmp;
    struct hostent h_ent;
    char mess[1400];

    remote_ip = (127 << 24) + 1; /* 127.0.0.1 */
    // mcast_address = (224 << 24) + 1; /* 224.0.0.1 */

    gethostname(local_name, sizeof(local_name)); // find the host name
    h_tmp = gethostbyname(local_name);           // find host information
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
        if (argc > 2)
        {
            print_usage();
            return (1);
        }
        sscanf(argv[1], "%s", remote_name);
        h_tmp = gethostbyname(remote_name);
        remote_ip = h_tmp->h_addr_list[0];
        if (h_tmp == NULL)
        {
            print_usage();
            return (1);
        }
        memcpy(&h_ent, h_tmp, sizeof(h_ent));
        memcpy(&remote_ip, h_ent.h_addr, sizeof(remote_ip));
        remote_ip = ntohl(remote_ip);
        format_ip(temp, remote_ip);
        printf("Remote Host Name: %s (%s)\n", remote_name, temp);
    }

    int protocol = 1; // unreliable
    sk = spines_socket(PF_SPINES, SOCK_DGRAM, protocol, daemon_ptr);
    if (ret < 0)
    {
        printf("socket error\n");
        exit(1);
    }

    /*
    name.sin_family = AF_INET;
    name.sin_addr.s_addr = INADDR_ANY;
    name.sin_port = htons(SEND_PORT);
    if (spines_bind(sk, (struct sockaddr *)&name, sizeof(name)) < 0)
    {
        perror("err: bind");
        exit(1);
    }
    */

    host.sin_family = AF_INET;
    host.sin_port = htons(RECV_PORT);
    memcpy(&h_ent, gethostbyname(remote_name), sizeof(h_ent));
    memcpy(&host.sin_addr, h_ent.h_addr, sizeof(host.sin_addr));

    i = 0;
    memset(mess, 0, 1400);
    strcpy(mess, "Hello World :)");
    while (1)
    {
        // mcast_address = (225 << 24) + 1;
        // sprintf(mess, "%d", address);
        ret = spines_sendto(sk, mess, strlen(mess) + 1, 0, (struct sockaddr *)&host, sizeof(struct sockaddr));
        // ret = spines_sendto(sk, mcast_address, RECV_PORT, mess, strlen(mess) + 1);
        if (ret < 0)
        {
            printf("send error\n");
            exit(1);
        }
        printf("Sent packet %d: \"%s\"\n", i++, mess);

        sleep(1);
    }
}

void print_usage(void)
{
    printf("Usage: ./adv_sender <remote_name>\n");
}
