document.addEventListener('DOMContentLoaded', () => {
    fetchRoles();
    fetchUserList();

    document.getElementById('prevPage').classList.add('disabled');

    document.getElementById('prevPage').addEventListener('click', () => {
        let currentPage = parseInt(document.querySelector('input[name="page"]').value);
        if (currentPage > 1) {
            updatePage(currentPage - 1);
        }
    });

    document.getElementById('nextPage').addEventListener('click', () => {
        let currentPage = parseInt(document.querySelector('input[name="page"]').value);
        updatePage(currentPage + 1);
    });

    document.getElementById('pageSizeSelect').addEventListener('change', (event) => {
        updatePageSize(event.target.value);
    });

    document.getElementById('searchForm').addEventListener('submit', (event) => {
        event.preventDefault();
        fetchUserList();
    });
});

function updatePage(newPage) {
    document.querySelector('input[name="page"]').value = newPage;
    document.getElementById('currentPage').innerText = newPage;
    fetchUserList();
}

function updatePageSize(newPageSize) {
    document.querySelector('input[name="page"]').value = 1;
    document.querySelector('select[name="pageSize"]').value = newPageSize;
    document.getElementById('currentPage').innerText = 1;
    fetchUserList();
}

function fetchUserList() {
    const filter = document.querySelector('input[name="filter"]').value;
    const page = document.querySelector('input[name="page"]').value;
    const pageSize = document.querySelector('select[name="pageSize"]').value;

    const url = `/Users/GetAllUsersList?filter=${filter}&page=${page}&pageSize=${pageSize}`;

    fetch(url)
        .then(response => {
            if (!response.ok) {
                return response.text().then(text => { throw new Error(text) });
            }
            return response.json();
        })
        .then(data => {
            updateTable(data);
            const currentPage = parseInt(page);

            if (currentPage === 1) {
                document.getElementById('prevPage').classList.add('disabled');
            } else {
                document.getElementById('prevPage').classList.remove('disabled');
            }

            const nextPage = currentPage + 1;
            const nextUrl = `/Users/GetAllUsersList?filter=${filter}&page=${nextPage}&pageSize=${pageSize}`;

            fetch(nextUrl)
                .then(response => response.json())
                .then(nextData => {
                    if (nextData.length === 0) {
                        document.getElementById('nextPage').classList.add('disabled');
                    } else {
                        document.getElementById('nextPage').classList.remove('disabled');
                    }
                })
                .catch(error => {
                    document.getElementById('nextPage').classList.remove('disabled');
                });
        })
        .catch(error => console.error('Error fetching user list:', error));
}



function updateTable(users) {
    const tableBody = document.getElementById('userTableBody');
    tableBody.innerHTML = '';

    users.forEach(user => {
        const row = document.createElement('tr');
        row.id = `userRow_${user.id}`;
        row.innerHTML = `
                    <td class="tableData" name="fullname">${user.fullname}</td>
                    <td class="tableData" name="username">${user.username}</td>
                    <td class="tableData" name="email">${user.email}</td>
                    <td class="tableData" name="role">${user.role}</td>
                    <td class="tableData" id="actions">
                        <a href="#" title="Edit" onclick="openEditUserModal('${user.id}', '${user.fullname}', '${user.username}', '${user.email}', '${user.role}', ${user.active})">
                            <img src="/images/edit-icon.png" alt="Edit" class="action-icon">
                        </a>
                        <a href="#" title="Delete" onclick="deleteUser('${user.id}')">
                            <img src="/images/delete-icon.jpg" alt="Delete" class="action-icon">
                        </a>
                    </td>
                `;
        tableBody.appendChild(row);
    });
}


function openEditUserModal(id, fullname, username, email, role, active) {
    document.getElementById('editUserId').value = id;
    document.getElementById('editFullname').value = fullname;
    document.getElementById('editUsername').value = username;
    document.getElementById('editEmail').value = email;
    document.getElementById('editRole').value = role;
    document.getElementById('editActive').checked = active;

    document.getElementById('editUserModal').style.display = 'block';
}

function closeEditUserModal() {
    document.getElementById('editUserModal').style.display = 'none';
}

function submitEditUser() {
    const formData = new FormData(document.getElementById('editUserForm'));
    const userId = formData.get('userId');
    const fullname = formData.get('fullname');
    const username = formData.get('username');
    const email = formData.get('email');
    const role = formData.get('role');
    const active = formData.get('active');

    const user = {
        id: userId,
        fullname: fullname,
        username: username,
        email: email,
        role: role,
        active: active
    };

    fetch(`/Users/UpdateUser`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(user)
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to update user.');
            }
            return response.json();
        })
        .then(data => {
            closeEditUserModal();
            fetchUserList();
            displaySuccessMessage('User updated successfully.');
        })
        .catch(error => {
            displayErrorMessage('Failed to update user.');
        });
}

function deleteUser(userId) {
    if (confirm('Are you sure you want to delete this user?')) {
        fetch(`/Users/DeleteUser?userId=${userId}`, {
            method: 'DELETE'
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Failed to delete user.');
                }
                return response.json();
            })
            .then(data => {
                fetchUserList();
                displaySuccessMessage('User deleted successfully.');
            })
            .catch(error => {
                displayErrorMessage('Failed to delete user.');
            });
    }
}

function fetchRoles() {
    fetch(`/Users/GetRoles`)
        .then(response => response.json())
        .then(roles => {
            const selectRole = document.getElementById('editRole');
            selectRole.innerHTML = '';

            roles.forEach(role => {
                const option = document.createElement('option');
                option.value = role;
                option.textContent = role;
                selectRole.appendChild(option);
            });
        })
        .catch(error => {
            console.error('Error fetching roles:', error);
        });
}

function displaySuccessMessage(message) {
    const successMessage = document.querySelector('.successMessage');
    successMessage.textContent = message;
    successMessage.style.display = 'block';

    setTimeout(() => {
        successMessage.style.display = 'none';
    }, 3000);
}

function displayErrorMessage(message) {
    const errorMessage = document.querySelector('.errorMessage');
    errorMessage.textContent = message;
    errorMessage.style.display = 'block';

    setTimeout(() => {
        errorMessage.style.display = 'none';
    }, 3000);
}

function sortTable(columnName, sortOrder) {
    const asc = sortOrder === 'asc' ? 1 : -1;
    const tbody = document.getElementById('userTableBody');
    const rows = Array.from(tbody.querySelectorAll('tr'));

    rows.sort((rowA, rowB) => {
        const cellA = rowA.querySelector(`[name="${columnName}"]`).textContent.trim();
        const cellB = rowB.querySelector(`[name="${columnName}"]`).textContent.trim();

        if (cellA < cellB) return -asc;
        if (cellA > cellB) return asc;
        return 0;
    });
    rows.forEach(row => tbody.removeChild(row));
    rows.forEach(row => tbody.appendChild(row));
}