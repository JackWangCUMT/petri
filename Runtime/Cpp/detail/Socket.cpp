/**
 * This source file was orinally written in 2005 by Lionel Fuentes. It has since been modified, and
 * is provided as-is and any kind of warranty is disclaimed by the author.
 */

#include "Socket.h"
#include <cassert>
#include <cstdio>
#include <cstring>
#include <fcntl.h>
#include <memory>
#include <stdexcept>

namespace Petri {

    Socket::Socket() {
        _fd = socket(AF_INET,
                     SOCK_STREAM,
                     0); // AF_INET : internet; SOCK_STREAM : par flux; 0 : protocol (TCP)
        int reuse = 0;
        if(_fd >= 0) {
            int enable = 1;
            reuse = setsockopt(_fd, SOL_SOCKET, SO_REUSEADDR, &enable, sizeof(int));
        }

        if(_fd < 0 || reuse < 0) {
            throw std::runtime_error("Impossible de créer le socket !");
        }
    }

    Socket::~Socket() {
        if(_fd <= 0) {
            return;
        }

        shutdown();
        close(_fd);
    }

    bool Socket::setBlocking(bool blocking) {
        bool ok = true;
        int flags = fcntl(_fd, F_GETFL, 0);
        if(flags < 0) {
            ok = false;
        }
        if(ok) {
            flags = blocking ? (flags & ~O_NONBLOCK) : (flags | O_NONBLOCK);
            ok = fcntl(_fd, F_SETFL, flags) == 0;
        }

        return ok;
    }

    // Connexion au serveur
    bool Socket::connect(const char *server_name, uint16_t port) {
        if(_fd <= 0)
            return false;

        hostent *hostinfo;
        // Recupere l'adresse ip correspond a l'adresse du serveur
        hostinfo = gethostbyname(server_name);
        // Impossible de recuperer l'addresse IP <=> impossible de se connecter, on sort
        if(hostinfo == nullptr) {
            return false;
        }


        // On remplit la structure _addr
        _addr.sin_family = AF_INET;   // Adresse de type internet : on doit toujours mettre ca
        _addr.sin_port = htons(port); // Port
        _addr.sin_addr = *(struct in_addr *)hostinfo->h_addr; // Adresse IP du serveur
        memset(&(_addr.sin_zero), 0, 8);                      // On met le reste (8 octets) a 0

        if(::connect(_fd, (struct sockaddr *)&_addr, sizeof(struct sockaddr_in)) < 0) {
            _state = SOCK_FREE;
        } else {
            _state = SOCK_CONNECTED;
        }

        return _state == SOCK_CONNECTED;
    }

    // Envoi de donnees (d'un serveur vers un client) :
    ssize_t Socket::send(Socket &client_socket, const void *data, size_t nb_bytes) {
        if(_state != SOCK_LISTENING) {
            return -1;
        }

        return ::send(client_socket._fd, data, nb_bytes, 0);
    }

    // Reception de donnees (donnees allant d'un client vers un serveur) :
    ssize_t Socket::receive(Socket &client_socket, void *buffer, size_t max_bytes) {
        if(_state != SOCK_LISTENING) {
            return -1;
        }

        return recv(client_socket._fd, buffer, max_bytes, 0);
    }

    // Envoi d'un paquet (d'un serveur vers un client)
    // A la difference de Send(), on rajoute un header de 4 octets indiquant la taille
    // du paquet. Un SendMsg() correspond a un ReceiveMsg().
    bool Socket::sendMsg(Socket &client_socket, const void *data, size_t nb_bytes) {
        uint8_t header[4] = {uint8_t((nb_bytes >> 0 * 8) & 0xFF),
                             uint8_t((nb_bytes >> 1 * 8) & 0xFF),
                             uint8_t((nb_bytes >> 2 * 8) & 0xFF),
                             uint8_t((nb_bytes >> 3 * 8) & 0xFF)};

        ssize_t remaining = nb_bytes;

        ssize_t n = 0;
        if((n = this->send(client_socket, (const void *)header, 4)) <= 0) {
            return false;
        }

        while(remaining != 0) {
            n = this->send(client_socket, (void *)(&((uint8_t *)data)[nb_bytes - remaining]), remaining);
            if(n <= 0) {
                return false;
            }

            remaining = remaining - n;
        }

        return true;
    }

    // Pareil que ReceiveMsg mais alloue la memoire necessaire
    std::vector<uint8_t> Socket::receiveNewMsg(Socket &client_socket) {
        uint8_t header[4];
        size_t nb_bytes = 0; // Nombre d'octets dans le paquet
        size_t remaining;

        ssize_t received = this->receive(client_socket, (void *)header, 4); // Lecture du header

        // Cas ou l'on n'a pas recu assez d'octets pour avoir un header complet :
        if(received < 4) {
            return {};
        }

        nb_bytes = ((int)(header[0]) << 0 * 8) + ((int)(header[1]) << 1 * 8) +
                   ((int)(header[2]) << 2 * 8) + ((int)(header[3]) << 3 * 8);

        // On alloue de la memoire pour les donnees :
        std::vector<uint8_t> data(nb_bytes);

        // On lit les octets qui doivent etre lus :
        remaining = nb_bytes;
        while(remaining != 0) {
            remaining =
            remaining - this->receive(client_socket, (void *)(&data[nb_bytes - remaining]), remaining);
        }

        return data;
    }

    // Mise sur ecoute (pour un serveur) :
    bool Socket::listen(uint16_t port) {
        // On remplit la structure _addr, qui correspond a l'adresse du serveur (nous)
        _addr.sin_family = AF_INET;   // Adresse de type internet : on doit toujours mettre ca
        _addr.sin_port = htons(port); // Port
        _addr.sin_addr.s_addr =
        htonl(INADDR_ANY); // On ecoute tout le monde, quelle que soit son adresse IP
        memset(&(_addr.sin_zero), 0, sizeof(_addr.sin_zero)); // On met le reste (8 octets) a 0

        // On associe la socket a un port et a une adresse, definis dans server_addr :
        if(bind(_fd, (struct sockaddr *)&_addr, sizeof(struct sockaddr_in)) < 0) {
            perror("SockErr");
            fprintf(stderr, "Erreur lors de l'association de la socket avec l'adresse\n");
            _state = SOCK_FREE;
        } else {
            // On se met sur ecoute
            if(::listen(_fd, 1) < 0) {
                perror("SockErr");
                fprintf(stderr, "Erreur lors de la mise sur écoute\n");
                _state = SOCK_FREE;
            } else {
                _state = SOCK_LISTENING;
            }
        }

        return _state == SOCK_LISTENING;
    }

    // Acceptation d'un nouveau client (pour un serveur)
    bool Socket::accept(Socket &sock_client) {
        // On remplit la structure _addr, qui correspond a l'adresse du serveur (nous)
        sock_client._addr.sin_family =
        AF_INET;                        // Adresse de type internet : on doit toujours mettre ca
        sock_client._addr.sin_port = 0; // Port
        sock_client._addr.sin_addr.s_addr =
        htons(INADDR_ANY); // On ecoute tout le monde, quelle que soit son adresse IP
        memset(&(sock_client._addr.sin_zero), 0, 8); // On met le reste (8 octets) a 0

        socklen_t sin_size = sizeof(struct sockaddr_in);
        sock_client._fd = ::accept(_fd, (struct sockaddr *)&(sock_client._addr), &sin_size);

        if(sock_client._fd != -1) {
            sock_client._state = SOCK_ACCEPTED;
            return true;
        } else {
            return false;
        }
    }

    // Ferme la connexion
    void Socket::shutdown() {
        if(_state != SOCK_FREE) {
            ::shutdown(_fd, SHUT_RDWR);
            _state = SOCK_FREE;
        }
    }
}
