/**
 * This source file was orinally written in 2005 by Lionel Fuentes. It has since been modified, and
 * is provided as-is and any kind of warranty is disclaimed by the author.
 */

#ifndef Petri_Socket_h
#define Petri_Socket_h

#include <arpa/inet.h>
#include <netdb.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <sys/types.h>
#include <unistd.h>

#include <cstdint>
#include <utility>
#include <vector>

namespace Petri {

    class Socket {
    public:
        Socket();
        ~Socket();

        // Enumeration des etats ou peut etre une Socket.
        enum SockState { SOCK_FREE = 0, SOCK_CONNECTED, SOCK_LISTENING, SOCK_ACCEPTED };

        SockState getState() {
            return _state;
        }

        // Connexion a un serveur :
        // -server_adress : addresse du serveur auquel on se connecte
        // -port : numero du port utilise
        bool connect(const char *server_adress, uint16_t port);

        // Envoi de donnees (d'un serveur vers un client) :
        // -client_socket : pointeur vers la Socket correspondant au client a qui envoyer
        // les donnees (obtenu via Accept())
        // -data : pointeur vers les donnees
        // -nb_bytes : taille des donnees a envoyer, en octets
        ssize_t send(Socket &client_socket, const void *data, std::size_t nb_bytes);


        // Reception de donnees (donnees allant d'un client vers un serveur) :
        // -client_socket : socket du client qui nous envoie les donnees
        // -buffer : pointeur vers l'endroit ou l'on doit stocker les donnees
        // -max_bytes : taille du buffer en octets, nombre maximal de donnees pouvant
        // etre retournees
        ssize_t receive(Socket &client_socket, void *buffer, std::size_t max_bytes);

        // Envoi d'un paquet (d'un serveur vers un client) :
        // -client_socket : pointeur vers la Socket correspondant au client a qui envoyer
        // les donnees (obtenu via Accept())
        // -data : pointeur vers les donnees
        // -nb_bytes : taille des donnees a envoyer, en octets
        // A la difference de Send(), on rajoute un header de 4 octets indiquant la taille
        // du paquet. Un SendMsg() correspond a un ReceiveMsg().
        bool sendMsg(Socket &client_socket, const void *data, std::size_t nb_bytes);

        std::vector<uint8_t> receiveNewMsg(Socket &client_socket);


        // Mise sur ecoute (pour un serveur) :
        // -port : numero du port a ecouter
        // -max_queue : nombre maximum de clients pouvant attendre dans une file
        bool listen(uint16_t port);

        // Acceptation d'un nouveau client (pour un serveur)
        // -sock_client : pointeur vers la socket correspondant au nouveau client accepte
        bool accept(Socket &sock_client);

        // Ferme la connexion
        void shutdown();

    private:
        Socket(const Socket &ref) = delete;
        Socket &operator=(Socket ref) = delete;

        int _fd = 0;
        SockState _state = SOCK_FREE;
        sockaddr_in _addr = {};
    };
}

#endif // SOCKET_H
