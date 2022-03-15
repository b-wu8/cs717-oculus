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
#include <stdbool.h>

#define SPINES_PORT 8100
#define MAX_BYTES 100000
#define MAX_NUM_PLAYERS 16
#define MAX_NUM_SESSIONS 16
#define MAX_TOTAL_PLAYERS (MAX_NUM_PLAYERS + MAX_NUM_SESSIONS)
#define DEFAULT_TIMEOUT_SEC 10  // 10 seconds until player timeout
#define RESPONSE_SIZE 2048

#define SYN "1" // client declares his existence and wants to join a room
#define INPUT "2" // client has new data
#define FIN "3" // client requests to leave lobby
#define HEARTBEAT "4" // client heartbeat (also a ping)
#define DATA "5"  // server response with consistent data
#define LOBBY "6" // server response to players giving lobby info

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

struct Transform {
    struct Position pos;
    struct Quaternion quat;
};

struct Joystick {
    float x;
    float z;
};

struct Controller {
    struct Transform transform;
    struct Joystick joystick;
};

struct Avatar {
    struct Position offset;
    struct Transform head;
    struct Controller left;
    struct Controller right;
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
    struct timeval timestamp;
    char* ping;
};

struct Session {
    char lobby[64];
    int num_players;
    int has_changed;
    int timeout_sec;
    double timestep;
    struct Player players[MAX_NUM_PLAYERS];  // Dynamically allocate memory later
    struct Sphere sphere;
    struct Plane plane;
};

struct SessionManager {
    struct Session sessions[MAX_NUM_SESSIONS];
    int num_sessions;
    double timestep;
    int sk;
};

void format_addr(char *buf, struct sockaddr_in* addr);
void format_ip(char *buf, int ip);
void print_local_addr();
int setup_recv_socket(int* sk, char* ip, char* port);
int create_or_join_session(struct SessionManager* session_manager, char* mess, struct sockaddr_in* addr);
int format_response(char* response, struct Session* session);
int get_player_session(struct Session** session, struct SessionManager* session_manager, char* mess);
int handle_player_input(struct SessionManager* session_manager, char* mess);
int handle_player_heartbeat(struct SessionManager* session_manager, char* mess);
int handle_player_leave(struct SessionManager* session_manager, char* mess);
int check_timeouts(struct SessionManager* session_manager);
int apply_movement_step(struct Session* session);
void move_sphere(struct Sphere* sphere, int timeout_ms);

int main(int argc, char *argv[])
{
    int sk;
    struct sockaddr_in remote_addr;
    socklen_t remote_len;
    struct timeval timeout;
    fd_set mask, temp_mask;
    char mess[MAX_BYTES];

    // Verify command line invocation
    if(argc != 4){
        printf("Usage: ./oculus_server <ip> <port> <time_step (default=10ms)>\n");
        exit(1);
    }

    if (setup_recv_socket(&sk, argv[1], argv[2])) {
        exit(1);  // Error is printed within setup_recv_socket()
    }

    int timeout_ms = atoi(argv[3]);
    if (timeout_ms <= 0) {
        printf("time_step of %d ms is invalid\n", timeout_ms);
        exit(1);
    }
    timeout.tv_sec = timeout_ms / 1000;
    timeout.tv_usec = (timeout_ms * 1000) % 1000000;

    // Server forloop variables
    struct sockaddr_in* addr;
    struct Session* session;
    struct timeval loop_timeout;
    struct SessionManager session_manager;
    memset(&session_manager, 0, sizeof(session_manager));
    session_manager.timestep = ((double) timeout_ms) / 1000;
    session_manager.sk = sk;
    char addr_str_buff[256];
    memset(addr_str_buff, 0, 256);
    char response_buff[RESPONSE_SIZE];
    memset(response_buff, 0, RESPONSE_SIZE);
    int num, bytes, i, j;
    
    // Create temporary buffers for print outs
    printf("Awaiting messages from Oculus...\n");

    // Tbh, I still don't know how masks work
    FD_ZERO(&mask);
    FD_SET(sk, &mask);
    memcpy(&loop_timeout, &timeout, sizeof(struct timeval));
    for (;;)
    { 
        // Receive packet
        temp_mask = mask;
        num = select(FD_SETSIZE, &temp_mask, NULL, NULL, &loop_timeout);

        if (num > 0) {  // Received message
            bytes = recvfrom(sk, mess, sizeof(mess), 0, (struct sockaddr *)&remote_addr, &remote_len);
            mess[bytes] = 0;  // Ad null-terminator to string (soemtimes this is redundant)

            // Display that we have received message
            format_addr(addr_str_buff, &remote_addr);
            printf("rain1 received %d byte message from %s: %s", bytes, addr_str_buff, mess);
            if (bytes && mess[bytes - 1] != '\n')
                printf("\n");

            if (strncmp(mess, SYN, 1) == 0)  // FIXME
                if (create_or_join_session(&session_manager, mess, &remote_addr) != 0)
                    continue;  // We should send back an error response later

            if (strncmp(mess, FIN, 1) == 0)  // FIXME
                if (handle_player_leave(&session_manager, mess) != 0)
                    continue;  // We should send back an error response later

            if (strncmp(mess, INPUT, 1) == 0)
                if (handle_player_input(&session_manager, mess) != 0)
                    continue;
            
            if (strncmp(mess, HEARTBEAT, 1) == 0)
                if (handle_player_heartbeat(&session_manager, mess) != 0)
                    continue;

        } else { // timeout
            check_timeouts(&session_manager);
            for (i = 0; i < session_manager.num_sessions; i++) {
                session = &session_manager.sessions[i];
                //apply_movement_step(session);
                if (session->has_changed) {
                    format_response(response_buff, session);
                    // printf("Response (len=%d): %s\n", (int) strlen(response_buff), response_buff);
                    for (j = 0; j < session->num_players; j++) {
                        addr = &(session->players[j].addr);
                        sendto(sk, response_buff, strlen(response_buff), 0, (struct sockaddr*)addr, sizeof(struct sockaddr_in));

                        // Print address we are responding to
                        format_addr(addr_str_buff, addr);
                        printf("Responded to %s\n", addr_str_buff);
                    }
                    session->has_changed = 0;
                }
                move_sphere(&session->sphere, timeout_ms);
                session->has_changed = 1;  // Moved ball
            }
            memcpy(&loop_timeout, &timeout, sizeof(struct timeval));  // Reset loop timeout
        }
    }
}

void move_sphere(struct Sphere* sphere, int timeout_ms) {
    sphere->t += 2 * M_PI / 15 * timeout_ms / 1000;  // rotate at 15 rev per sec
    sphere->pos.x = 4 * cos(sphere->t);
    sphere->pos.y = 2 * sin(sphere->t / 4 + 1) + 4;
    sphere->pos.z = 4 * sin(sphere->t);
}

int check_timeouts(struct SessionManager* session_manager) {    
    struct timeval current_time;
    gettimeofday(&current_time, NULL);

    char removal_messages[MAX_TOTAL_PLAYERS][128];
    memset(removal_messages, 0, 16 * 128);
    int num_timeouts = 0;
    struct Session* session;

    for (int i = 0; i < session_manager->num_sessions; i++) {
        session = &session_manager->sessions[i];
        for (int j = 0; j < session->num_players; j++) 
            if (current_time.tv_sec - session->players[j].timestamp.tv_sec > session->timeout_sec)
                sprintf(removal_messages[num_timeouts++], "%s %s %s", FIN, session->players[j].name, session->lobby);
    }
    
    if (num_timeouts > 0)
        printf("%d playeys timed out\n", num_timeouts);
    for (int i = 0; i < num_timeouts; i++)
        handle_player_leave(session_manager, removal_messages[i]);
    
    return num_timeouts;
}

//static int count = 0;

int apply_movement_step(struct Session* session) {
    struct Avatar* avatar;

    // float x_hat, z_hat, hat_mag, w_prime, forward_step, right_step;
    for (int j = 0; j < session->num_players; j++) {
        avatar = &session->players[j].avatar;
        /*
        w_prime = sqrt(1 - pow(avatar->head.quat.w, 2));
        x_hat = avatar->head.quat.x / w_prime;
        z_hat = avatar->head.quat.z / w_prime;
        if (count++ % 10 == 0) {
            printf("qx = %0.3f  qy = %0.3f  qz = %0.3f  qw = %0.3f\n", avatar->right.transform.quat.x, avatar->right.transform.quat.y, avatar->right.transform.quat.z, avatar->right.transform.quat.w);
            w_prime = sqrt(1 - pow(avatar->right.transform.quat.w, 2));
            printf("x  = %0.3f  y  = %0.3f  z  = %0.3f\n", avatar->right.transform.quat.x / w_prime, avatar->right.transform.quat.y / w_prime, avatar->right.transform.quat.z / w_prime);
            //printf("qx = %0.3f  qy = %0.3f  qz = %0.3f  qw = %0.3f\n", avatar->head.quat.x, avatar->head.quat.y, avatar->head.quat.z, avatar->head.quat.w);
            //printf("x  = %0.3f  y  = %0.3f  z  = %0.3f\n", x_hat, avatar->head.quat.y / w_prime, z_hat);
        }
        hat_mag = sqrt(pow(x_hat, 2) + pow(z_hat, 2));
        x_hat /= hat_mag;
        z_hat /= hat_mag;
        if (isnanf(x_hat) || isinff(x_hat) || isnanf(z_hat) || isinff(z_hat)) {  
            printf("Couldn't resolve quaternion [x, y, z, w] = [%0.3f, %0.3f, %0.3f, %0.3f]\n",
                avatar->head.quat.x, avatar->head.quat.y, avatar->head.quat.z, avatar->head.quat.w);
            continue;
        }
        if (fabs(avatar->right.joystick.z) > 0.2) {
            forward_step = avatar->right.joystick.z * session->timestep;
            avatar->offset.x += forward_step * x_hat;
            avatar->offset.z += forward_step * z_hat;
        }
        if (fabs(avatar->right.joystick.x) > 0.2) {
            // Geometric rotation 90 deg clockwise in xz plane
            right_step = avatar->right.joystick.x * session->timestep;
            avatar->offset.x += right_step * z_hat;
            avatar->offset.z += -right_step * x_hat;
        }
        */
       
        if (fabs(avatar->right.joystick.z) > 0.2)
            avatar->offset.z += avatar->right.joystick.z * session->timestep;
        if (fabs(avatar->right.joystick.x) > 0.2) 
            avatar->offset.x += avatar->right.joystick.x * session->timestep;
    }
    return 0;
}

int handle_player_heartbeat(struct SessionManager* session_manager, char* mess) {
    char dummy_type[64];
    char name[64];
    char dummy_lobby[64];
    sscanf(mess, "%s %s %s", dummy_type, name, dummy_lobby);

    struct Session* session;
    int ret;
    if ((ret = get_player_session(&session, session_manager, mess) != 0))
        return ret;

    for (int i = 0; i < session->num_players; i++)
        if (strcmp(session->players[i].name, name) == 0) {
            gettimeofday(&session->players[i].timestamp, NULL);
            sendto(session_manager->sk, mess, strlen(mess), 0, (struct sockaddr*)&session->players[i].addr, sizeof(struct sockaddr_in));
            return 0;
        }
    return 1;
}

int handle_player_input(struct SessionManager* session_manager, char* mess) {
    char dummy_type[64];
    char name[64];
    char lobby[64];
    int offset = 3;
    sscanf(mess, "%s %s %s", dummy_type, name, lobby);

    struct Session* session;
    int ret;
    if ((ret = get_player_session(&session, session_manager, mess) != 0))
        return ret;

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
    for (k = 0; k < 128 - offset; k++) {
        numbers[k] = atof(number_strings[k + offset]);
    }

    int player_idx = -1;
    for (k = 0; k < session->num_players; k++) {
        if (strcmp(session->players[k].name, name) == 0) {
            player_idx = k;
            break;
        }
    }

    struct Player* player = &(session->players[player_idx]);
    player->avatar.head.pos.x = numbers[0];
    player->avatar.head.pos.y = numbers[1];
    player->avatar.head.pos.z = numbers[2];
    player->avatar.head.quat.x = numbers[3];
    player->avatar.head.quat.y = numbers[4];
    player->avatar.head.quat.z = numbers[5];
    player->avatar.head.quat.w = numbers[6];

    player->avatar.left.transform.pos.x = numbers[7];
    player->avatar.left.transform.pos.y = numbers[8];
    player->avatar.left.transform.pos.z = numbers[9];
    player->avatar.left.transform.quat.x = numbers[10];
    player->avatar.left.transform.quat.y = numbers[11];
    player->avatar.left.transform.quat.z = numbers[12];
    player->avatar.left.transform.quat.w = numbers[13];

    player->avatar.right.transform.pos.x = numbers[14];
    player->avatar.right.transform.pos.y = numbers[15];
    player->avatar.right.transform.pos.z = numbers[16];
    player->avatar.right.transform.quat.x = numbers[17];
    player->avatar.right.transform.quat.y = numbers[18];
    player->avatar.right.transform.quat.z = numbers[19];
    player->avatar.right.transform.quat.w = numbers[20];

    player->avatar.left.joystick.x = numbers[21];
    player->avatar.left.joystick.z = numbers[22];
    player->avatar.right.joystick.x = numbers[23];
    player->avatar.right.joystick.z = numbers[24];

    player->ping = number_strings[25 + offset];
    
    /*
    for (k = 0; k < 16; k++) {
        printf("%s --- %6.3f\n", number_strings[k+3], numbers[k]);
    }
    */

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
    struct Transform* transform;
    struct Position* position;
    char* ptr;

    memset(response, 0, RESPONSE_SIZE);
    sprintf(response, "%s %s %d\n", DATA, session->lobby, session->num_players);

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
        transform = &(player->avatar.head);
        sprintf(ptr, "%0.3f %0.3f %0.3f %0.3f %0.3f %0.3f %0.3f ", 
            transform->pos.x, transform->pos.y, transform->pos.z, transform->quat.x, transform->quat.y, transform->quat.z, transform->quat.w);
        
        ptr = &(response[strlen(response)]); 
        transform = &(player->avatar.left.transform);
        sprintf(ptr, "%0.3f %0.3f %0.3f %0.3f %0.3f %0.3f %0.3f ", 
            transform->pos.x, transform->pos.y, transform->pos.z, transform->quat.x, transform->quat.y, transform->quat.z, transform->quat.w);
        
        ptr = &(response[strlen(response)]);  
        transform = &(player->avatar.right.transform);
        sprintf(ptr, "%0.3f %0.3f %0.3f %0.3f %0.3f %0.3f %0.3f ", 
            transform->pos.x, transform->pos.y, transform->pos.z, transform->quat.x, transform->quat.y, transform->quat.z, transform->quat.w);

        ptr = &(response[strlen(response)]);  
        position = &(player->avatar.offset);
        sprintf(ptr, "%0.3f %0.3f %0.3f ", position->x, position->y, position->z);

        ptr = &(response[strlen(response)]);  
        sprintf(ptr, "%s\n", player->ping);
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
            session->players[session->num_players].avatar.offset.y = 1.5;  // Make everyone the same height ?
            gettimeofday(&session->players[session->num_players].timestamp, NULL);
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
    move_sphere(&session->sphere, 1);
    session->timestep = session_manager->timestep;
    session->plane.pos.x = session->plane.pos.y = session->plane.pos.z = 0;
    session->has_changed = 1;
    session->timeout_sec = DEFAULT_TIMEOUT_SEC;

    // Add player to lobby
    struct Player* player = &session->players[session->num_players++]; 
    memcpy(&(player->addr), addr, sizeof(struct sockaddr_in));
    strcpy(player->name, name);
    player->avatar.offset.y = 1.5;  // Make everyone the same height ?
    gettimeofday(&player->timestamp, NULL);

    printf("Player \"%s\" created lobby \"%s\" (%d/%d)\n", name, lobby, session_manager->num_sessions, MAX_NUM_SESSIONS);
    return 0;
}

int handle_player_leave(struct SessionManager* session_manager, char* mess) {
    char dummy_type[64];
    char name[64];
    char lobby[64];
    sscanf(mess, "%s %s %s", dummy_type, name, lobby);

    for (int i = 0; i < session_manager->num_sessions; i++) {
        if (strcmp(lobby, session_manager->sessions[i].lobby) == 0) {
            for (int j = 0; j < session_manager->sessions[i].num_players; j++) {
                if (strcmp(session_manager->sessions[i].players[j].name, name) == 0) {
                    struct Session* session = &(session_manager->sessions[i]);
                    for (int k = j; k < session->num_players - 1; k++)
                        memcpy(&session->players[k], &session->players[k+1], sizeof(struct Player));
                    memset(&session->players[session->num_players - 1], 0, sizeof(struct Player));
                    session->num_players--;
                    session->has_changed = 1;
                    printf("Player \"%s\" left lobby \"%s\" (%d/%d)\n", name, lobby, session->num_players, MAX_NUM_PLAYERS);

                    if (session->num_players == 0) {
                        for (int k = i; k < session_manager->num_sessions - 1; k++)
                            memcpy(&session_manager->sessions[k], &session_manager->sessions[k+1], sizeof(struct Session));
                        memset(&session_manager->sessions[session_manager->num_sessions - 1], 0, sizeof(struct Session));
                        session_manager->num_sessions--;
                        printf("Lobby \"%s\" has been closed\n", lobby);
                    } else {
                        char leave_message[256];
                        char current_message[250];
                        sprintf(current_message, "%s %s %d", LOBBY, session->lobby, session->num_players);
                        for (int k = 0; k < session->num_players; k++) {
                            sprintf(leave_message, "%s %s", current_message, session->players[k].name);
                            memcpy(current_message, leave_message, 250);
                        }
                        printf("MESSAGE: %s\n", leave_message);
                        for (int k = 0; k < session->num_players; k++) 
                            sendto(session_manager->sk, leave_message, strlen(leave_message), 0, 
                                (struct sockaddr*)&session->players[k].addr, sizeof(struct sockaddr_in));
                    }

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
