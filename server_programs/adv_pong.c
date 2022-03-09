<<<<<<< HEAD
/*
#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <errno.h>
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <netdb.h>

#define MAXBUFLEN 100

// get sockaddr, IPv4 or IPv6:
void *get_in_addr(struct sockaddr *sa)
{
    if (sa->sa_family == AF_INET) {
        return &(((struct sockaddr_in*)sa)->sin_addr);
    }

    return &(((struct sockaddr_in6*)sa)->sin6_addr);
}

int main(int argc, char *argv[])
{
    int sockfd;
    struct addrinfo hints, *servinfo, *p;
    int rv;
    int numbytes;
    struct sockaddr_storage their_addr;
    char buf[MAXBUFLEN];
    socklen_t addr_len;
    char s[INET6_ADDRSTRLEN];
=======
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
#include "math.h"

#define SPINES_PORT 8100
#define MAX_BYTES 100000

void format_addr(char *buf, struct sockaddr_in addr);
void format_ip(char *buf, int ip);
void print_local_addr();

struct RainPacket {
    int flag;
    double x;
    double y;
    double z;
    char   mess[256];
};

int main(int argc, char *argv[])
{
    int sk;
    struct sockaddr_in local_addr, oculus_addr, remote_addr;
    socklen_t oculus_len, remote_len;
    fd_set mask, dummy_mask, temp_mask;
    char mess[MAX_BYTES];

    // Verify command line invocation
    if(argc != 2){
        fprintf(stderr, "please specify the port to listen");
    }

    const char * MYPORT = argv[1];
    int rv;
    struct addrinfo hints, *servinfo, *p;
>>>>>>> f18d5011ef42dfb4a30faa1e896b87374fb4dcf9

    memset(&hints, 0, sizeof hints);
    hints.ai_family = AF_INET; // set to AF_INET to use IPv4
    hints.ai_socktype = SOCK_DGRAM;
    hints.ai_flags = AI_PASSIVE; // use my IP

<<<<<<< HEAD
    if(argc != 2){
        fprintf(stderr, "please specify the port to listen");
    }
    
    const char * MYPORT = argv[1];

=======
>>>>>>> f18d5011ef42dfb4a30faa1e896b87374fb4dcf9
    if ((rv = getaddrinfo(NULL, MYPORT, &hints, &servinfo)) != 0) {
        fprintf(stderr, "getaddrinfo: %s\n", gai_strerror(rv));
        return 1;
    }

    // loop through all the results and bind to the first we can
    for(p = servinfo; p != NULL; p = p->ai_next) {
<<<<<<< HEAD
        if ((sockfd = socket(p->ai_family, p->ai_socktype,
                             p->ai_protocol)) == -1) {
            perror("listener: socket");
            continue;
        }

        if (bind(sockfd, p->ai_addr, p->ai_addrlen) == -1) {
            close(sockfd);
            perror("listener: bind");
            continue;
=======
        if ((sk = socket(p->ai_family, p->ai_socktype,
                             p->ai_protocol)) == -1) {
            perror("adv_pong: socket");
            continue;
        }

        if (bind(sk, p->ai_addr, p->ai_addrlen) == -1) {
            close(sk);
            perror("adv_pong: bind for recv socket failed\n");
            continue;
        } else {
            printf("Receiving on port: %d\n", MYPORT);
>>>>>>> f18d5011ef42dfb4a30faa1e896b87374fb4dcf9
        }

        break;
    }

    if (p == NULL) {
<<<<<<< HEAD
        fprintf(stderr, "listener: failed to bind socket\n");
=======
        fprintf(stderr, "adv_pong: couldn't open recv socket\n");
>>>>>>> f18d5011ef42dfb4a30faa1e896b87374fb4dcf9
        return 2;
    }

    freeaddrinfo(servinfo);

<<<<<<< HEAD
    fprintf( stderr, "listener: waiting to recvfrom at port %s ......", MYPORT);

    addr_len = sizeof their_addr;
    if ((numbytes = recvfrom(sockfd, buf, MAXBUFLEN-1 , 0,
                             (struct sockaddr *)&their_addr, &addr_len)) == -1) {
        perror("recvfrom");
        exit(1);
    }
    fprintf( stderr, "listener: got packet from %s\n",
             inet_ntop(their_addr.ss_family,
                       get_in_addr((struct sockaddr *)&their_addr),
                       s, sizeof s));
    fprintf( stderr, "listener: packet is %d bytes long\n", numbytes);
    buf[numbytes] = '\0';
    fprintf( stderr, "listener: packet contains \"%s\"\n", buf);

    close(sockfd);

    return 0;
}
*/


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
#include "math.h"

#define SPINES_PORT 8100
#define RECV_PORT 4577
#define MAX_BYTES 100000
=======
    // Print local address (to verify we are where we think we are)
    print_local_addr();

    // packet should contain all data meant to be sent to oculus
    // This will be deprecated eventually
    struct RainPacket packet;
    memset(&packet, 0, sizeof(struct RainPacket));
    double t = 0;
    double r = 4;
    packet.flag = 1;
    packet.x = r * cos(t * 2 * M_PI);
    packet.z = r * sin(t * 2 * M_PI);
    packet.y = sin(t * 2 * M_PI / 16) + 2;
    
    // Tbh, I still don't know how masks work
    FD_ZERO(&mask);
    FD_ZERO(&dummy_mask);
    FD_SET(sk, &mask);
    printf("Awaiting messages from Oculus...\n");
    char addr_str_buff[256];
    memset(addr_str_buff, 0, 256);
    char response_buff[1024];
    memset(response_buff, 0, 1024);
    int num, bytes;
    for (;;)
    {   
        // Receive packet
        temp_mask = mask;
        num = select(FD_SETSIZE, &temp_mask, &dummy_mask, &dummy_mask, NULL);
        bytes = recvfrom(sk, mess, sizeof(mess), 0, (struct sockaddr *)&remote_addr, &remote_len);
        mess[bytes] = 0;  // Ad null-terminator to string (soemtimes this is redundant)

        // Display that we have received message
        format_addr(addr_str_buff, remote_addr);
        printf("rain1 received %d byte message from %s: %s", bytes, addr_str_buff, mess);
        if (bytes && mess[bytes - 1] != '\n')
            printf("\n");

        // Check if this is oculus server
        if (strcmp(mess, "OCULUS") == 0) {  // FIXME
            memcpy(&oculus_addr, &remote_addr, sizeof(struct sockaddr));
        }

        // Format response
        sprintf(response_buff, "%0.3f %0.3f %0.3f %s\n", packet.x, packet.y, packet.z, addr_str_buff);
        sendto(sk, response_buff, strlen(response_buff) + 1, 0, (struct sockaddr*)&oculus_addr, sizeof(oculus_addr));

        // Print address we are responding to
        format_addr(addr_str_buff, oculus_addr);
        printf("Responded to %s\n", addr_str_buff);

        // Update sphere position
        t += 0.05;
        packet.flag = 1;
        packet.x = r * cos(t * 2 * M_PI);
        packet.z = r * sin(t * 2 * M_PI);
        packet.y = sin(t * 2 * M_PI / 4) + 2;
    }
}
>>>>>>> f18d5011ef42dfb4a30faa1e896b87374fb4dcf9

void format_addr(char *buf, struct sockaddr_in addr)
{
    int ip = addr.sin_addr.s_addr;
    int port = ntohs(addr.sin_port);
    unsigned char bytes[4];
    bytes[0] = ip & 0xFF;
    bytes[1] = (ip >> 8) & 0xFF;
    bytes[2] = (ip >> 16) & 0xFF;
    bytes[3] = (ip >> 24) & 0xFF;
    sprintf(buf, "%d.%d.%d.%d:%d", bytes[3], bytes[2], bytes[1], bytes[0], port);
}

<<<<<<< HEAD
void format_ip(char *buf, int ip)
{
    unsigned char bytes[4];
    bytes[0] = ip & 0xFF;
    bytes[1] = (ip >> 8) & 0xFF;
    bytes[2] = (ip >> 16) & 0xFF;
    bytes[3] = (ip >> 24) & 0xFF;
    sprintf(buf, "%d.%d.%d.%d", bytes[3], bytes[2], bytes[1], bytes[0]);
=======
void format_ip(char* buf, int ip)
{
    sprintf(buf, "%d.%d.%d.%d", (ip >> 24) & 0xFF, (ip >> 16) & 0xFF, (ip >> 8) & 0xFF, ip & 0xFF);
>>>>>>> f18d5011ef42dfb4a30faa1e896b87374fb4dcf9
}

void print_local_addr()
{
    struct hostent *h_tmp;
    char local_name[256];
    char temp[256];
    int local_ip;

    gethostname(local_name, sizeof(local_name)); // find the host name
    h_tmp = gethostbyname(local_name);           // find host information
    memcpy(&local_ip, h_tmp->h_addr, sizeof(local_ip));
    local_ip = ntohl(local_ip);
    format_ip(temp, local_ip);
    printf("Local Host Name: %s (%s)\n", local_name, temp);
<<<<<<< HEAD
}

void print_usage(void);

struct RainPacket {
    int flag;
    double x;
    double y;
    double z;
    char   mess[256];
};

int main(int argc, char *argv[])
{
    int sk;
    int num;
    int bytes;
    struct sockaddr_in name;
    struct sockaddr_in from_addr;
    int from_ip, from_port;
    socklen_t from_len;
    fd_set mask, dummy_mask, temp_mask;
    char mess[MAX_BYTES];
    char temp[1024];

    print_local_addr();

    sk = socket(AF_INET, SOCK_DGRAM, 0);
    if (sk < 0)
    {
        printf("adv_pong: couldn't open recv socket\n");
        exit(1);
    }

    int host_ip = (128 << 24) + (220 << 16) + (221 << 8) + 21;  // 128.220.221.21
    name.sin_family = AF_INET;
    // name.sin_addr.s_addr = host_ip;
    name.sin_addr.s_addr = htonl(host_ip);
    // name.sin_addr.s_addr = htonl(inet_addr("128.220.221.21"));
    // name.sin_addr.s_addr = INADDR_ANY;
    name.sin_port = htons(RECV_PORT);

    if (bind(sk, (struct sockaddr *)&name, sizeof(name)) < 0)
    {
        printf("adv_pong: bind for recv socket failed\n");
        exit(1);
    }
    else
    {
        printf("Receiving on port: %d\n", RECV_PORT);
    }

    FD_ZERO(&mask);
    FD_ZERO(&dummy_mask);
    FD_SET(sk, &mask);

    struct RainPacket packet;
    memset(&packet, 0, sizeof(struct RainPacket));
    double t = 0;
    double r = 1;
    packet.flag = 1;
    packet.x = cos(t * 2 * M_PI);
    packet.z = sin(t * 2 * M_PI);
    packet.y = sin(t * 2 * M_PI / 4) + 2;

    printf("Awaiting ping message...\n");
    for (;;)
    {
        temp_mask = mask;
        num = select(FD_SETSIZE, &temp_mask, &dummy_mask, &dummy_mask, NULL);

        bytes = recvfrom(sk, mess, sizeof(mess), 0, (struct sockaddr *)&from_addr, &from_len);
        mess[bytes] = 0;
        format_addr(temp, from_addr);
        printf("Received %d byte message from %s\n", bytes, temp);

        // Respond to Unity server
        from_addr.sin_port = htons(8051);
        sprintf(packet.mess, "rain1 received message from %s\n", temp);

        char temp2[1024];
        memset(temp2, 0, 1024);
        sprintf(temp2, "%0.3f %0.3f %0.3f %s\n", packet.x, packet.y, packet.z, temp);
        sendto(sk, temp2, strlen(temp2) + 1, 0, (struct sockaddr*)&from_addr, sizeof(from_addr));

        // Print address we are responding to
        format_addr(temp, from_addr);
        printf("Responded to %s\n", temp);

        // Update sphere position
        t += 0.1;
        packet.flag = 1;
        packet.x = r * cos(t * 2 * M_PI);
        packet.z = r * sin(t * 2 * M_PI);
        packet.y = sin(t * 2 * M_PI / 4) + 2;
    }
}

void print_usage(void)
{
    printf("Usage: ./adv_pong\n");
}
=======
}
>>>>>>> f18d5011ef42dfb4a30faa1e896b87374fb4dcf9
