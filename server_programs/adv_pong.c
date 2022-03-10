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
#define MAX_NUM_PLAYERS 16
#define MAX_NUM_SESSIONS 16
#define RESPONSE_SIZE 2048
#define SYN "1" // first message from client
#define INPUT "2" // client update message
#define FIN "3" // client's explicite exit message

struct Position {
    float x;
    float y;
    float z;
};

struct Quaternion {
    float x;
    float y;
    float z;
    float w;
};

struct Pose {
    struct Position pos;
    struct Quaternion quat;
};

struct Avatar {
    struct Pose headset;
    struct Pose left_hand;
    struct Pose right_hand;
};

struct Sphere {
    float t;
    struct Position pos;
};

struct Plane {
    struct Position pos;
};

struct Player {
    char name[64];
    struct Avatar avatar;
    struct sockaddr_in addr;
};

struct Session {
    char lobby[64];
    int num_players;
    struct Player players[MAX_NUM_PLAYERS];  // Dynamically allocate memory later
    struct Sphere sphere;
    struct Plane plane;
};

struct SessionManager {
    struct Session sessions[MAX_NUM_SESSIONS];
    int num_sessions;
};

void format_addr(char *buf, struct sockaddr_in* addr);
void format_ip(char *buf, int ip);
void print_local_addr();
int setup_recv_socket(int* sk, char* ip, char* port);
int create_or_join_session(struct SessionManager* session_manager, char* mess, struct sockaddr_in* addr);
int format_response(char* response, struct Session* session);
int get_player_session(struct Session** session, struct SessionManager* session_manager, char* mess);
int update_player_position(struct Session* session, char* mess);

int main(int argc, char *argv[])
{
    int sk;
    struct sockaddr_in remote_addr;
    socklen_t remote_len;
    fd_set mask, dummy_mask, temp_mask;
    char mess[MAX_BYTES];

    // Verify command line invocation
    if(argc != 3){
        printf("Usage: ./oculus_server <ip> <port>");
        exit(1);
    }

    if (setup_recv_socket(&sk, argv[1], argv[2])) {
        exit(1);  // Error is printed within setup_recv_socket()
    }

    // Server forloop variables
    struct sockaddr_in* addr;
    struct Session* session;
    struct SessionManager session_manager;
    memset(&session_manager, 0, sizeof(session_manager));
    char addr_str_buff[256];
    memset(addr_str_buff, 0, 256);
    char response_buff[RESPONSE_SIZE];
    memset(response_buff, 0, RESPONSE_SIZE);
    int num, bytes, i;
    
    // Create temporary buffers for print outs
    printf("Awaiting messages from Oculus...\n");

    // Tbh, I still don't know how masks work
    FD_ZERO(&mask);
    FD_ZERO(&dummy_mask);
    FD_SET(sk, &mask);
    for (;;)
    {   
        // Receive packet
        temp_mask = mask;
        num = select(FD_SETSIZE, &temp_mask, &dummy_mask, &dummy_mask, NULL);
        bytes = recvfrom(sk, mess, sizeof(mess), 0, (struct sockaddr *)&remote_addr, &remote_len);
        mess[bytes] = 0;  // Ad null-terminator to string (soemtimes this is redundant)

        // Display that we have received message
        format_addr(addr_str_buff, &remote_addr);
        printf("rain1 received %d byte message from %s: %s", bytes, addr_str_buff, mess);
        if (bytes && mess[bytes - 1] != '\n') {
            printf("\n");
        }

        if (strlen(mess) < 6) {
            printf("Skipping short message...\n");
            continue;
        }

        // Check if this is oculus server
        if (strcmp(mess[0], SYN) == 0) {  // FIXME
            if (create_or_join_session(&session_manager, mess, &remote_addr) != 0) {
                continue;  // We should send back an error response later
            }
        }

        if (get_player_session(&session, &session_manager, mess) != 0) {
            continue;
        }

        if (strcmp(mess[0], INPUT) == 0) {
            if (update_player_position(session, mess) != 0) {
                continue;
            }
        }

        // todo: player exit :if (atoi(mess[0]) == FIN) 

        // Format response
        format_response(response_buff, session);
        printf("Response (len=%d): %s\n", (int) strlen(response_buff), response_buff);
        for (i = 0; i < session->num_players; i++) {
            addr = &(session->players[i].addr);
            sendto(sk, response_buff, strlen(response_buff) + 1, 0, (struct sockaddr*)addr, sizeof(struct sockaddr_in));

            // Print address we are responding to
            format_addr(addr_str_buff, addr);
            printf("Responded to %s\n", addr_str_buff);
        }

        session->sphere.t += 0.05;
        session->sphere.pos.x = 4 * cos(session->sphere.t * 2 * M_PI);
        session->sphere.pos.y = sin(session->sphere.t * 2 * M_PI / 16) + 3;
        session->sphere.pos.z = 4 * sin(session->sphere.t * 2 * M_PI);
    }
}

int update_player_position(struct Session* session, char* mess) {
    char dummy_type[64];
    char name[64];
    char lobby[64];
    sscanf(mess, "%s %s %s", dummy_type, name, lobby);

    // Extract numbers from message
    char number_strings[128][32];
    memset(number_strings, 0, 32 * 128);
    int i, j, k;
    i = j = k = 0;
    int num_chars = strlen(mess);
    for (k = 0; k < num_chars; k++) {
        if (mess[k] != ' ') {
            number_strings[i][j++] = mess[k];
        } else {
            j = 0;
            i++;
        }
    }
    float numbers[125];  // ignore type, name, lobby
    for (k = 0; k < 128 - 3; k++) {
        numbers[k] = atof(number_strings[k + 3]);
    }

    int player_idx = -1;
    for (k = 0; k < session->num_players; k++) {
        if (strcmp(session->players[k].name, name) == 0) {
            player_idx = k;
            break;
        }
    }

    struct Player* player = &(session->players[player_idx]);
    player->avatar.headset.pos.x = numbers[0];
    player->avatar.headset.pos.y = numbers[1];
    player->avatar.headset.pos.z = numbers[2];
    player->avatar.headset.quat.x = numbers[3];
    player->avatar.headset.quat.y = numbers[4];
    player->avatar.headset.quat.z = numbers[5];
    player->avatar.headset.quat.w = numbers[6];

    for (k = 0; k < 16; k++) {
        printf("%s --- %6.3f\n", number_strings[k+3], numbers[k]);
    }

    return 0;
}

int get_player_session(struct Session** session, struct SessionManager* session_manager, char* mess) {
    char dummy_type[64];
    char name[64];
    char lobby[64];
    sscanf(mess, "%s %s %s", dummy_type, name, lobby);

    for (int i = 0; i < session_manager->num_sessions; i++) {
        if (strcmp(lobby, session_manager->sessions[i].lobby) == 0) {
            for (int j = 0; j < session_manager->sessions[i].num_players; j++) {
                if (strcmp(session_manager->sessions[i].players[j].name, name) == 0) {
                    *session = &(session_manager->sessions[i]);
                    printf("Found player \"%s\" in lobby \"%s\" (%d/%d)\n", name, lobby, (*session)->num_players, MAX_NUM_PLAYERS);
                    return 0;
                }
            }
            printf("Player \"%s\" is not in lobby \"%s\"\n", name, lobby);
            return 1;
        }
    }
    printf("Lobby \"%s\" does not exist\n", lobby);
    return 1;
}

int format_response(char* response, struct Session* session) {
    struct Player* player;
    struct Pose* pose;
    char* ptr;

    memset(response, 0, RESPONSE_SIZE);
    sprintf(response, "%s %d\n", session->lobby, session->num_players);

    // Format sphere data
    ptr = &(response[strlen(response)]);  // point to next writable char in response
    sprintf(ptr, "SPHERE %0.3f %0.3f %0.3f\n", session->sphere.pos.x, session->sphere.pos.y, session->sphere.pos.z);

    // Format plane data
    ptr = &(response[strlen(response)]);
    sprintf(ptr, "PLANE %0.3f %0.3f %0.3f\n", session->plane.pos.x, session->plane.pos.y, session->plane.pos.z);

    for (int i = 0; i < session->num_players; i++) {
        player = &(session->players[i]);
        ptr = &(response[strlen(response)]);
        sprintf(ptr, "%s ", player->name);

        ptr = &(response[strlen(response)]);
        pose = &(player->avatar.headset);
        sprintf(ptr, "%0.3f %0.3f %0.3f %0.3f %0.3f %0.3f %0.3f ", pose->pos.x, pose->pos.y, pose->pos.z, pose->quat.x, pose->quat.y, pose->quat.z, pose->quat.w);
        
        ptr = &(response[strlen(response)]); 
        pose = &(player->avatar.left_hand);
        sprintf(ptr, "%0.3f %0.3f %0.3f %0.3f %0.3f %0.3f %0.3f ", pose->pos.x, pose->pos.y, pose->pos.z, pose->quat.x, pose->quat.y, pose->quat.z, pose->quat.w);
        
        ptr = &(response[strlen(response)]);  
        pose = &(player->avatar.right_hand);
        sprintf(ptr, "%0.3f %0.3f %0.3f %0.3f %0.3f %0.3f %0.3f\n", pose->pos.x, pose->pos.y, pose->pos.z, pose->quat.x, pose->quat.y, pose->quat.z, pose->quat.w);
    }
    response[strlen(response) - 1] = '\0';  // remove last newline
    return 0;
}

int create_or_join_session(struct SessionManager* session_manager, char* mess, struct sockaddr_in* addr) {
    // Parse message
    char oculus[64];
    char name[64];
    char lobby[64];
    sscanf(mess, "%s %s %s", oculus, name, lobby);

    struct Session* session;
    for (int i = 0; i < session_manager->num_sessions; i++) {
        session = &(session_manager->sessions[i]);
        if (strcmp(lobby, session->lobby) == 0) {
            for (int j = 0; j < session->num_players; j++) {
                if (strcmp(name, session->players[j].name) == 0) {
                    printf("Player \"%s\" is already in lobby \"%s\" (%d/%d)\n", name, lobby, session->num_players, MAX_NUM_PLAYERS);
                    memcpy(&(session->players[j].addr), addr, sizeof(struct sockaddr_in));  // might have to update address
                    return 0;
                }
            }
            // Only make it here if we are not in lobby
            if (session->num_players == MAX_NUM_PLAYERS) {
                printf("Player \"%s\" cannot join lobby \"%s\" (%d/%d)\n", name, lobby, MAX_NUM_PLAYERS, MAX_NUM_PLAYERS);
                return 1;
            }
            memcpy(&session->players[session->num_players].addr, addr, sizeof(struct sockaddr_in));
            strcpy(session->players[session->num_players].name, name);
            session->num_players++;
            printf("Player \"%s\" joined lobby \"%s\" (%d/%d)\n", name, lobby, session->num_players, MAX_NUM_PLAYERS);
            return 0;
        }
    }

    // Only make it here if no lobby exists with the given name
    if (session_manager->num_sessions == MAX_NUM_SESSIONS) {
        printf("Player \"%s\" cannot create lobby \"%s\". Max number of sessions are open (%d/%d)\n", name, lobby, MAX_NUM_SESSIONS, MAX_NUM_SESSIONS);
        return 2;
    }

    // Create lobby
    session = &(session_manager->sessions[session_manager->num_sessions++]);
    strcpy(session->lobby, lobby);
    session->sphere.t = 0.05;
    session->sphere.pos.x = 4 * cos(session->sphere.t * 2 * M_PI);
    session->sphere.pos.y = sin(session->sphere.t * 2 * M_PI / 16) + 3;
    session->sphere.pos.z = 4 * sin(session->sphere.t * 2 * M_PI);
    session->plane.pos.x = session->plane.pos.y = session->plane.pos.z = 0;

    // Add player to lobby
    memcpy(&(session->players[session->num_players].addr), addr, sizeof(struct sockaddr_in));
    strcpy(session->players[session->num_players].name, name);
    session->num_players++;
    printf("Player \"%s\" created lobby \"%s\" (%d/%d)\n", name, lobby, session_manager->num_sessions, MAX_NUM_SESSIONS);
    return 0;
}

int setup_recv_socket(int* sk, char* ip, char* port) {
    int ret;
    struct addrinfo hints, *servinfo, *p;

    memset(&hints, 0, sizeof hints);
    hints.ai_family = AF_INET; // set to AF_INET to use IPv4
    hints.ai_socktype = SOCK_DGRAM;
    // hints.ai_flags = AI_PASSIVE; // use my IP

    // Get address info
    if ((ret = getaddrinfo(ip, port, &hints, &servinfo)) != 0) {
        fprintf(stderr, "getaddrinfo: %s\n", gai_strerror(ret));
        return 1;
    }

    // loop through all the results and bind to the first we can
    for(p = servinfo; p != NULL; p = p->ai_next) {
        if ((*sk = socket(p->ai_family, p->ai_socktype, p->ai_protocol)) == -1) {
            perror("adv_pong: socket");
            continue;
        }
        if (bind(*sk, p->ai_addr, p->ai_addrlen) == -1) {
            close(*sk);
            perror("adv_pong: bind for recv socket failed\n");
            continue;
        } else {
            printf("Receiving on %s:%s\n", ip, port);
        }
        break;
    }
    if (p == NULL) {
        fprintf(stderr, "adv_pong: couldn't open recv socket\n");
        return 2;
    }
    freeaddrinfo(servinfo);

    return 0;
}

void format_addr(char *buf, struct sockaddr_in* addr)
{
    int ip = addr->sin_addr.s_addr;
    int port = ntohs(addr->sin_port);
    unsigned char bytes[4];
    bytes[0] = ip & 0xFF;
    bytes[1] = (ip >> 8) & 0xFF;
    bytes[2] = (ip >> 16) & 0xFF;
    bytes[3] = (ip >> 24) & 0xFF;
    sprintf(buf, "%d.%d.%d.%d:%d", bytes[3], bytes[2], bytes[1], bytes[0], port);
}

void format_ip(char* buf, int ip)
{
    sprintf(buf, "%d.%d.%d.%d", (ip >> 24) & 0xFF, (ip >> 16) & 0xFF, (ip >> 8) & 0xFF, ip & 0xFF);
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
}
