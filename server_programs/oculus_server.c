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
#define MAX_MESS_LEN 2048
#define MAX_NUM_PLAYERS 16
#define MAX_NUM_SESSIONS 16
#define MAX_STRING_LEN 64 
#define MAX_TOTAL_PLAYERS (MAX_NUM_PLAYERS + MAX_NUM_SESSIONS)
#define DEFAULT_TIMEOUT_SEC 10  // 10 seconds until player timeout
#define RESPONSE_SIZE 2048

// Represents position of unity object as (x,y,z) vector 
struct Position {
    float x;
    float y;
    float z;
};

// Represents rotation of unity object as (x,y,z,w) quaternion
// MAKE SURE QUATERNION FORMAT IS (X,Y,Z,W), NOT (W,X,Y,Z)
struct Quaternion {
    float x;
    float y;
    float z;
    float w;
};

// Transform is defined as a pair of position and rotation
struct Transform {
    struct Position pos;
    struct Quaternion quat;
};

// Controller joystick position represented by (x.y) vector
// The magnitude of this vector implies how far the stick is moved
struct Joystick {
    float x;
    float y;
};

// Controller is currently a transform and a joystick. 
// More features will be added to the joystick
struct Controller {
    struct Transform transform;
    struct Joystick joystick;
};

// Avatar objects are rendered on each of the clients. One for each
// player. The head, left, and right are all used to construct the 
// avatar on the client side. The offset gives the avatars coarse-grained 
// position in the game. 
struct Avatar {
    struct Position offset;
    struct Transform head;
    struct Controller left;
    struct Controller right;
};

// REMOVEME: sphere moving on a fixed path
struct Sphere {
    float t;
    struct Position pos;
};

// REMOVEME: plane in fixed location
struct Plane {
    struct Position pos;
};

struct Player {
    int id;
    char name[MAX_STRING_LEN];
    char client_ping_start[MAX_STRING_LEN];
    struct Avatar avatar;
    struct sockaddr_in addr;
    struct timeval timestamp;
};

struct Session {
    char lobby[MAX_STRING_LEN];
    int num_players;
    int has_changed;
    int timeout_sec;
    double timestep;
    struct Player players[MAX_NUM_PLAYERS];  // Dynamically allocate memory later
    struct Sphere sphere;
    struct Plane plane;
};

struct Context {
    struct Session sessions[MAX_NUM_SESSIONS];
    int num_sessions;
    double timestep;
    int sk;
};

enum Type {
    UNKNOWN = 0,
    SYN = 1,
    INPUT = 2,
    FIN = 3,
    HEARTBEAT = 4,
    DATA = 5,
    LOBBY = 6,
    TEST = 7
};

struct Request {
    enum Type type;
    struct sockaddr_in from_addr;
    int data_len;
    char player_name[MAX_STRING_LEN];
    char lobby[MAX_STRING_LEN];
    char data[MAX_MESS_LEN];
};

int parse_request(struct Request* request);
int format_response(char* response, struct Session* session);
int get_player_session(struct Session** session, struct Context* context, struct Request* request);
int remove_players_if_timeout(struct Context* context);
int apply_movement_step(struct Session* session);

// setup functions
int setup_recv_socket(int* sk, char* ip, char* port);
int setup_timeout(struct timeval* timeout, int* timeout_ms, char* tick_delay_ms);

// request handlers
int handle_player_join(struct Context* context, struct Request* request);
int handle_player_input(struct Context* context, struct Request* request);
int handle_player_heartbeat(struct Context* context, struct Request* request);
int handle_player_leave(struct Context* context, struct Request* request);

// simplicity functions
void move_sphere(struct Sphere* sphere, int timeout_ms);
struct Player* add_player_to_lobby(struct Session* session, struct Request* request);
struct Session* add_lobby_to_context(struct Context* context, struct Request* request);
void format_addr(char *buf, struct sockaddr_in* addr);

int main(int argc, char *argv[])
{
    int sk, timeout_ms;
    struct timeval timeout;
    fd_set mask, temp_mask;

    // Verify command line invocation
    if(argc != 4){
        printf("Usage: ./oculus_server <ip> <port> <tick_delay_ms>\n");
        exit(1);
    }

    if (setup_recv_socket(&sk, argv[1], argv[2]))
        exit(1);  // Error is printed within setup_recv_socket()

    if (setup_timeout(&timeout, &timeout_ms, argv[3]))
        exit(1);  // Error is printed within setup_timeout()

    // Server forloop variables
    struct sockaddr_in* addr;
    struct Session* session;
    struct timeval loop_timeout;
    socklen_t dummy_len;
    struct Request request;
    memset(&request, 0, sizeof(struct Request));
    struct Context context;
    memset(&context, 0, sizeof(struct Context));
    context.timestep = ((double) timeout_ms) / 1000;
    context.sk = sk;
    char addr_str_buff[256];
    char response_buff[RESPONSE_SIZE];
    int num, i, j;
    
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
            // Parse request from input bytes
            request.data_len = recvfrom(sk, request.data, MAX_MESS_LEN, 
                0, (struct sockaddr *)&request.from_addr, &dummy_len);
            parse_request(&request);

            // Display that we have received message
            format_addr(addr_str_buff, &request.from_addr);
            printf("received %d byte request from %s: %s", 
                request.data_len, addr_str_buff, request.data);
            if (request.data_len && request.data[request.data_len - 1] != '\n')
                printf("\n");

            switch (request.type) {
                case SYN:
                    handle_player_join(&context, &request);
                    break;
                case FIN:
                    handle_player_leave(&context, &request);
                    break;
                case INPUT:
                    handle_player_input(&context, &request);
                    break;
                case HEARTBEAT:
                    handle_player_heartbeat(&context, &request);
                    break;
                case TEST:
                    printf("%s\n", request.data);
                    break;
                default:
                    break;
            }

        } else { // timeout
            remove_players_if_timeout(&context);
            for (i = 0; i < context.num_sessions; i++) {
                session = &context.sessions[i];
                apply_movement_step(session);
                if (session->has_changed) {
                    format_response(response_buff, session);
                    printf("sending %d byte response to lobby \"%s\": %s\nSent to: ", 
                        (int) strlen(response_buff), session->lobby, response_buff);
                    for (j = 0; j < session->num_players; j++) {
                        addr = &(session->players[j].addr);
                        sendto(sk, response_buff, strlen(response_buff), 
                            0, (struct sockaddr*)addr, sizeof(struct sockaddr_in));

                        // Print address we are responding to
                        format_addr(addr_str_buff, addr);
                        printf("%s ", addr_str_buff);
                    }
                    printf("\n");
                    session->has_changed = 0;
                }
                move_sphere(&session->sphere, timeout_ms);
                session->has_changed = 1;  // Moved ball
            }
            memcpy(&loop_timeout, &timeout, sizeof(struct timeval));  // Reset loop timeout
        }
    }
}

int parse_request(struct Request* request) {
    request->data[request->data_len] = 0;  // make sure data is ended with null-terminator
    char type_string[MAX_STRING_LEN];
    sscanf(request->data, "%s %s %s", type_string, request->player_name, request->lobby);
    request->type = (enum Type) atoi(type_string);
    return 0;
}

void move_sphere(struct Sphere* sphere, int timeout_ms) {
    sphere->t += 2 * M_PI / 15 * timeout_ms / 1000;  // rotate at 15 rev per sec
    sphere->pos.x = 4 * cos(sphere->t);
    sphere->pos.y = 2 * sin(sphere->t / 4 + 1) + 4;
    sphere->pos.z = 4 * sin(sphere->t);
}

struct Player* add_player_to_lobby(struct Session* session, struct Request* request) {
    struct Player* player = &session->players[session->num_players];
    memcpy(&player->addr, &request->from_addr, sizeof(struct sockaddr_in));
    strcpy(player->name, request->player_name);
    player->avatar.offset.y = 1.5;  // Make everyone the same height ?
    gettimeofday(&player->timestamp, NULL);
    session->num_players++;
    printf("Player \"%s\" joined lobby \"%s\" (%d/%d)\n", 
        request->player_name, request->lobby, session->num_players, MAX_NUM_PLAYERS);
    return player;
}

struct Session* add_lobby_to_context(struct Context* context, struct Request* request) {
    struct Session* session = &(context->sessions[context->num_sessions]);
    strcpy(session->lobby, request->lobby);
    move_sphere(&session->sphere, 1);
    session->timestep = context->timestep;
    session->plane.pos.x = session->plane.pos.y = session->plane.pos.z = 0;
    session->has_changed = 1;
    session->timeout_sec = DEFAULT_TIMEOUT_SEC;
    context->num_sessions++;
    printf("Lobby \"%s\" has been created (0/%d)\n", request->lobby, MAX_NUM_PLAYERS);
    return session;
}

int remove_players_if_timeout(struct Context* context) {    
    struct timeval current_time;
    gettimeofday(&current_time, NULL);

    struct Request removal_requests[MAX_TOTAL_PLAYERS];
    memset(removal_requests, 0, sizeof(removal_requests));
    int num_timeouts = 0;
    struct Session* session;

    for (int i = 0; i < context->num_sessions; i++) {
        session = &context->sessions[i];
        for (int j = 0; j < session->num_players; j++) 
            if (current_time.tv_sec - session->players[j].timestamp.tv_sec > session->timeout_sec) {
                // Player j in session i has timed out
                sprintf(removal_requests[num_timeouts].data, "%d %s %s", (int) FIN, session->players[j].name, session->lobby);
                strcpy(removal_requests[num_timeouts].player_name, session->players[j].name);
                strcpy(removal_requests[num_timeouts].lobby, session->lobby);
                removal_requests[num_timeouts].data_len = strlen(removal_requests[num_timeouts].data);
                num_timeouts++;
            }
    }
    
    if (num_timeouts > 0)
        printf("%d players timed out\n", num_timeouts);
    for (int i = 0; i < num_timeouts; i++)
        handle_player_leave(context, &removal_requests[i]);
    
    return num_timeouts;
}

int apply_movement_step(struct Session* session) {
    struct Avatar* avatar;
    for (int j = 0; j < session->num_players; j++) {
        avatar = &session->players[j].avatar;      
        if (fabs(avatar->right.joystick.y) > 0.2)
            avatar->offset.z += avatar->right.joystick.y * session->timestep;
        if (fabs(avatar->right.joystick.x) > 0.2) 
            avatar->offset.x += avatar->right.joystick.x * session->timestep;
    }
    return 0;
}

int handle_player_heartbeat(struct Context* context, struct Request* request) {
    struct Session* session;
    int ret = get_player_session(&session, context, request);
    if (ret != 0) 
        // Couldn't find player
        return ret;

    for (int i = 0; i < session->num_players; i++)
        if (strcmp(session->players[i].name, request->player_name) == 0) {
            gettimeofday(&session->players[i].timestamp, NULL);
            sendto(context->sk, request->data, request->data_len, 0, (struct sockaddr*)&session->players[i].addr, sizeof(struct sockaddr_in));
            return 0;
        }
    return 1;
}

int handle_player_input(struct Context* context, struct Request* request) {
    struct Session* session;
    int ret = get_player_session(&session, context, request);
    if (ret != 0) 
        // Couldn't find player
        return ret;

    // FIXME: using space separated string aprsing is error-prone !!!!!

    // Extract numbers from message
    // This assumes that all fields are separated by exactly one space !!!!!
    char number_strings[128][32];
    memset(number_strings, 0, 32 * 128);
    int i, j, k;
    i = j = k = 0;
    int num_chars = strlen(request->data);
    for (k = 0; k < num_chars; k++)
        if (request->data[k] != ' ') {
            number_strings[i][j++] = request->data[k];
        } else {
            j = 0;
            i++;
        }

    int offset = 3;  // ignore type, name, lobby fields  
    float numbers[128 - offset];
    for (k = 0; k < 128 - offset; k++)
        numbers[k] = atof(number_strings[k + offset]);

    int player_idx = -1;
    for (k = 0; k < session->num_players; k++)
        if (strcmp(session->players[k].name, request->player_name) == 0) {
            player_idx = k;
            break;
        }
    if (player_idx == -1)
        return 3;

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
    player->avatar.left.joystick.y = numbers[22];
    player->avatar.right.joystick.x = numbers[23];
    player->avatar.right.joystick.y = numbers[24];

    strcpy(player->client_ping_start, number_strings[25 + offset]);

    return 0;
}

int get_player_session(struct Session** session, struct Context* context, struct Request* request) {
    for (int i = 0; i < context->num_sessions; i++) {
        if (strcmp(request->lobby, context->sessions[i].lobby) == 0) {
            for (int j = 0; j < context->sessions[i].num_players; j++) {
                if (strcmp(request->player_name, context->sessions[i].players[j].name) == 0) {
                    printf("Found player \"%s\" in lobby \"%s\" (%d/%d)\n", 
                        request->player_name, request->lobby, context->sessions[i].num_players, MAX_NUM_PLAYERS);
                    *session = &(context->sessions[i]);
                    return 0;
                }
            }
            printf("Player \"%s\" is not in lobby \"%s\"\n", request->player_name, request->lobby);
            *session = &(context->sessions[i]);
            return 1;
        }
    }
    printf("Lobby \"%s\" does not exist\n", request->lobby);
    return 2;
}

int format_response(char* response, struct Session* session) {
    struct Player* player;
    struct Transform* transform;
    struct Position* position;
    char* ptr;

    memset(response, 0, RESPONSE_SIZE);
    sprintf(response, "%d %s %d\n", (int) DATA, session->lobby, session->num_players);

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
        sprintf(ptr, "%s\n", player->client_ping_start);
    }
    response[strlen(response) - 1] = '\0';  // remove last newline
    return 0;
}

int handle_player_join(struct Context* context, struct Request* request) {
    struct Session* session;
    int ret = get_player_session(&session, context, request);
    if (ret == 0) {
        // Lobby exists and player is already in that lobby
        // We might still have to update remote address
        for (int i = 0; i < session->num_players; i++) 
            if (strcmp(session->players[i].name, request->player_name) == 0) 
                memcpy(&session->players[i].addr, &request->from_addr, sizeof(struct sockaddr_in));
        return 0;
    } else if (ret == 1) {
        // Lobby exists, but player is not currently in that lobby
        if (session->num_players == MAX_NUM_PLAYERS) {
            printf("Player \"%s\" cannot join lobby \"%s\" (%d/%d)\n", 
                request->player_name, request->lobby, MAX_NUM_PLAYERS, MAX_NUM_PLAYERS);
            return 1;
        }
        add_player_to_lobby(session, request);
        return 0;
    } else if (ret == 2) {
        // Lobby doesn't exist so we have to make a new one
        if (context->num_sessions == MAX_NUM_SESSIONS) {
            printf("Player \"%s\" cannot create lobby \"%s\". Max number of sessions are open (%d/%d)\n", 
                request->player_name, request->lobby, MAX_NUM_SESSIONS, MAX_NUM_SESSIONS);
            return 2;
        }

        session = add_lobby_to_context(context, request);
        add_player_to_lobby(session, request);
        return 0;
    }
    return ret;
}

int handle_player_leave(struct Context* context, struct Request* request) {
    struct Session* session;
    int ret = get_player_session(&session, context, request);
    if (ret != 0)
        // Couldn't find player or lobby
        return ret;
    
    // Remove player from lobby
    for (int i = 0; i < session->num_players; i++)
        if (strcmp(session->players[i].name, request->player_name) == 0)
            for (int j = i; j < session->num_players - 1; j++)
                memcpy(&session->players[j], &session->players[j+1], sizeof(struct Player));
    memset(&session->players[session->num_players - 1], 0, sizeof(struct Player));
    session->num_players--;
    session->has_changed = 1;
    printf("Player \"%s\" left lobby \"%s\" (%d/%d)\n", 
        request->player_name, request->lobby, session->num_players, MAX_NUM_PLAYERS);

    if (session->num_players == 0) {
        // If this was the last player, close the session
        for (int i = 0; i < context->num_sessions; i++)
            if (strcmp(context->sessions[i].lobby, session->lobby) == 0)
                for (int j = i; j < context->num_sessions - 1; j++)
                    memcpy(&context->sessions[j], &context->sessions[j+1], sizeof(struct Session));
        memset(&context->sessions[context->num_sessions - 1], 0, sizeof(struct Session));
        context->num_sessions--;
        printf("Lobby \"%s\" has been closed\n", request->lobby);
    } else {
        // Send message to all other players that player has left
        char leave_message[256];
        char current_message[250];
        sprintf(current_message, "%d %s %d", (int) LOBBY, session->lobby, session->num_players);
        for (int i = 0; i < session->num_players; i++) {
            sprintf(leave_message, "%s %s", current_message, session->players[i].name);
            memcpy(current_message, leave_message, 250);
        }
        printf("Sending leave messsage: %s\n", leave_message);
        for (int i = 0; i < session->num_players; i++) 
            sendto(context->sk, leave_message, strlen(leave_message), 0, 
                (struct sockaddr*)&session->players[i].addr, sizeof(struct sockaddr_in));
    }

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
            perror("oculus_server: socket");
            continue;
        }
        if (bind(*sk, p->ai_addr, p->ai_addrlen) == -1) {
            close(*sk);
            perror("oculus_server: bind for recv socket failed\n");
            continue;
        } else {
            printf("Receiving on %s:%s\n", ip, port);
        }
        break;
    }
    if (p == NULL) {
        fprintf(stderr, "oculus_server: couldn't open recv socket\n");
        return 2;
    }
    freeaddrinfo(servinfo);

    return 0;
}

int setup_timeout(struct timeval* timeout, int* timeout_ms, char* tick_delay_ms) {
    *timeout_ms = atoi(tick_delay_ms);
    if (*timeout_ms <= 0) {
        printf("time_step of %d ms is invalid\n", *timeout_ms);
        exit(1);
    }
    timeout->tv_sec = *timeout_ms / 1000;
    timeout->tv_usec = (*timeout_ms * 1000) % 1000000;
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
